using UnityEngine;

public class move : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Rigidbody2D rb;
    public attack attackscript;
    
    public float xinput;
    public float movespeed;
    public float Jumpforce;
    [SerializeField] private Animator anim;
    public bool moving;
    public bool runing;
    public float currenspeed;
    private float tra = 1;
    private bool tra1 = false;
    [SerializeField] private float dit;
    [SerializeField] private LayerMask whatisground;
    private bool isground;
    public bool mo;
    
  
    

    void Start()
    {




    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("moiving", moving);
        anim.SetBool("runing", runing);
        xinput = Input.GetAxisRaw("Horizontal");
        float currenspeed = movespeed;
        mo = attackscript.isrushattack;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currenspeed = movespeed * 2;
        }


        if ( mo!= true)
        {
            rb.linearVelocity = new Vector2(xinput * currenspeed, rb.linearVelocity.y);
        }
        if (Input.GetKeyDown(KeyCode.Space) && isground)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Jumpforce);
            
           
        }
        moving = rb.linearVelocity.x != 0;
        runing = currenspeed != movespeed;
        if (!tra1 && rb.linearVelocity.x < 0)
        {
            tra = tra * -1;
            tra1 = !tra1;
            transform.Rotate(0, 180, 0);
        }
        else if (tra1 && rb.linearVelocity.x > 0)
        {
            tra = tra * -1;
            tra1 = !tra1;
            transform.Rotate(0, 180, 0);
        }
        isground = Physics2D.Raycast(transform.position, Vector2.down, dit, whatisground);
      
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y - dit));
    }
}















