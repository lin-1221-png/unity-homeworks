using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>一局游戏的存档内容。以后要加东西，就往这里加 public 字段（JsonUtility 只认字段）。</summary>
[Serializable]
public class SaveData
{
    public int version = 1;        // 存档格式版本，以后改结构了靠它做兼容
    public string sceneName;       // 存在哪个场景
    public float playerX;
    public float playerY;
    public int currentHealth;
    public int maxHealth;
    public int attackPower;
    public int coins;
    public float playTime;         // 存档时的游戏时长（秒）
    public string savedAt;         // 存档时间，方便显示"上次保存于..."
}

/// <summary>
/// 存档系统的核心：读、写、删、抓取玩家状态。
/// 挂在场景里任意常驻物体（Main Camera）上可以开启"进游戏自动读档"；
/// 不挂也能用——SavePoint.cs 就是直接调这里的静态方法存档的。
/// 存档是 JSON，放在 Application.persistentDataPath 下，退出游戏不会丢。
/// </summary>
public class SaveSystem : MonoBehaviour
{
    [Header("玩家")]
    [Tooltip("要存档的玩家。留空会自动找场景里叫 player 的物体")]
    [SerializeField] private Transform player;

    [Header("行为")]
    [Tooltip("用第几个存档位（一般保持 0）")]
    [SerializeField, Min(0)] private int slot = 0;

    /// <summary>存档文件夹（%USERPROFILE%\AppData\LocalLow\公司名\产品名）</summary>
    public static string Folder => Application.persistentDataPath;

    /// <summary>某个存档位的完整路径</summary>
    public static string PathFor(int slot = 0)
    {
        return Path.Combine(Folder, $"save_{slot}.json");
    }

    /// <summary>这个存档位有没有存档</summary>
    public static bool HasSave(int slot = 0)
    {
        return File.Exists(PathFor(slot));
    }

    /// <summary>把数据写进磁盘。成功返回 true</summary>
    public static bool Write(SaveData data, int slot = 0)
    {
        if (data == null)
        {
            return false;
        }

        try
        {
            string path = PathFor(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            Debug.Log($"已保存到 {path}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"存档失败：{e.Message}");
            return false;
        }
    }

    /// <summary>读存档。没有存档或文件坏了返回 null</summary>
    public static SaveData Read(int slot = 0)
    {
        try
        {
            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path, Encoding.UTF8));
        }
        catch (Exception e)
        {
            Debug.LogError($"读档失败：{e.Message}");
            return null;
        }
    }

    /// <summary>删掉某个存档位的存档</summary>
    public static void DeleteSave(int slot = 0)
    {
        try
        {
            string path = PathFor(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"已删除存档 {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"删除存档失败：{e.Message}");
        }
    }

    /// <summary>把某个玩家的当前状态抓成一份存档数据</summary>
    public static SaveData Capture(Transform player)
    {
        if (player == null)
        {
            Debug.LogWarning("SaveSystem：没有玩家可以存档");
            return null;
        }

        CharacterStats stats = player.GetComponentInParent<CharacterStats>();

        SaveData data = new SaveData
        {
            sceneName = player.gameObject.scene.name,
            playerX = player.position.x,
            playerY = player.position.y,
            playTime = Time.time,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        if (stats != null)
        {
            data.currentHealth = stats.CurrentHealth;
            data.maxHealth = stats.MaxHealth;
            data.attackPower = stats.AttackPower;
            data.coins = stats.Coins;
        }

        return data;
    }

    /// <summary>存档：抓取玩家状态并写盘。任何地方都能调</summary>
    public static bool SaveGame(Transform player, int slot = 0)
    {
        SaveData data = Capture(player);
        return data != null && Write(data, slot);
    }

    /// <summary>读档：读盘并把状态套回玩家身上。任何地方都能调</summary>
    public static bool LoadGame(Transform player, int slot = 0)
    {
        SaveData data = Read(slot);
        if (data == null)
        {
            Debug.Log("SaveSystem：没有找到存档");
            return false;
        }

        if (player == null)
        {
            Debug.LogWarning("SaveSystem：没有玩家可以读档");
            return false;
        }

        // 位置
        player.position = new Vector3(data.playerX, data.playerY, player.position.z);

        // 清掉惯性，否则读档后会顺着旧速度飞出去
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        // 血量、攻击力、金币
        CharacterStats stats = player.GetComponentInParent<CharacterStats>();
        if (stats != null)
        {
            int health = data.currentHealth > 0 ? data.currentHealth : Mathf.Max(1, data.maxHealth);
            stats.SetMaxHealth(Mathf.Max(1, data.maxHealth), false);
            stats.SetHealth(health);
            stats.SetAttackPower(data.attackPower);
            stats.SetCoins(data.coins);
        }

        Debug.Log($"已读档（{data.savedAt}）");
        return true;
    }

    private Transform ResolvePlayer()
    {
        if (player == null)
        {
            GameObject found = GameObject.Find("player");
            if (found != null)
            {
                player = found.transform;
            }
        }

        return player;
    }

    private void Start()
    {
        // 进游戏自动读档：有存档才读，没有就正常开局
        ResolvePlayer();

        if (HasSave(slot))
        {
            Load();
        }
    }

    /// <summary>
    /// 场景里就算忘了挂 SaveSystem，也自动建一个，保证"进游戏直接读档"永远生效。
    /// 已经挂了的话不会重复创建。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<SaveSystem>() != null)
        {
            return;
        }

        GameObject go = new GameObject("SaveSystem");
        go.AddComponent<SaveSystem>();
    }

    /// <summary>存当前玩家的档</summary>
    public void Save()
    {
        SaveGame(ResolvePlayer(), slot);
    }

    /// <summary>读档到当前玩家</summary>
    public void Load()
    {
        LoadGame(ResolvePlayer(), slot);
    }
}
