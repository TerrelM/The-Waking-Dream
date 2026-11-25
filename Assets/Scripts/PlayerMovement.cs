using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerCollision collision;
    private InputSystem_Actions inputActions;

    #region Input Actions
    //Move Actions
    private Vector2 move;
    //Jump Actions
    private bool jump;
    private bool jumpPressed;
    private bool jumpReleased;
    //Attack Actions
    private bool attack;
    private bool attackPressed;
    private bool attackReleased;
    //Dash Actions
    private bool dash;
    private bool dashPressed;
    private bool dashReleased;
    //Menu Actions
    private bool menu;
    private bool menuPressed;
    private bool menuReleased;
    //Item Actions
    private bool item;
    private bool itemPressed;
    private bool itemReleased;
    //Confirm Actions
    private bool confirm;
    private bool confirmPressed;
    private bool confirmReleased;
    //Cancel Actions
    private bool cancel;
    private bool cancelPressed;
    private bool cancelReleased;
    #endregion



    private Vector2 velocity;
    
    
    private float walkSpeed;
    private float walkAcceleration;
    private float walkDeceleration;
    private float walkTurnAcceleration;
    private float airTurnAcceleration;

    private bool jumping;
    private float jumpStrong;
    private float jumpWeak;
    private float jumpReduction;
    private float fallSpeed;
    private float fallStrong;
    private float fallWeak;
    private float gravity;

    private bool grounded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        collision = GetComponent<PlayerCollision>();
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }
    private void Start()
    {
        walkSpeed = 10f;
        walkAcceleration = 30f;
        walkDeceleration = 30f;
        walkTurnAcceleration = 60f;
        airTurnAcceleration = 30f;

        jumping = false;
        jumpStrong = 15f;
        jumpWeak = 10f;
        jumpReduction = 0.7f;

        fallSpeed = 10f;
        fallStrong = 60f;
        fallWeak = 45f;

        gravity = fallStrong;
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        #region Get Inputs
        //Get Move Inputs
        move = inputActions.PlayerControls.Move.ReadValue<Vector2>();

        //Get Jump Inputs
        jump = inputActions.PlayerControls.Jump.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Jump.WasPressedThisDynamicUpdate()) { jumpPressed = true; }
        if (inputActions.PlayerControls.Jump.WasReleasedThisDynamicUpdate()) { jumpReleased = true; }

        //Get Attack
        attack = inputActions.PlayerControls.Attack.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Attack.WasPressedThisDynamicUpdate()) { attackPressed = true; }
        if (inputActions.PlayerControls.Attack.WasReleasedThisDynamicUpdate()) { attackReleased = true; }

        //Get Dash
        dash = inputActions.PlayerControls.Dash.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Dash.WasPressedThisDynamicUpdate()) { dashPressed = true; }
        if (inputActions.PlayerControls.Dash.WasReleasedThisDynamicUpdate()) { dashReleased = true; }

        //Get Menu
        menu = inputActions.PlayerControls.Menu.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Menu.WasPressedThisDynamicUpdate()) { menuPressed = true; }
        if (inputActions.PlayerControls.Menu.WasReleasedThisDynamicUpdate()) { menuReleased = true; }

        //Get Item
        item = inputActions.PlayerControls.Item.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Item.WasPressedThisDynamicUpdate()) { itemPressed = true; }
        if (inputActions.PlayerControls.Item.WasReleasedThisDynamicUpdate()) { itemReleased = true; }

        //Get Confirm
        confirm = inputActions.PlayerControls.Confirm.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Confirm.WasPressedThisDynamicUpdate()) { confirmPressed = true; }
        if (inputActions.PlayerControls.Confirm.WasReleasedThisDynamicUpdate()) { confirmReleased = true; }

        //Get Cancel
        cancel = inputActions.PlayerControls.Cancel.ReadValue<float>() == 1;
        if (inputActions.PlayerControls.Cancel.WasPressedThisDynamicUpdate()) { cancelPressed = true; }
        if (inputActions.PlayerControls.Cancel.WasReleasedThisDynamicUpdate()) { cancelReleased = true; }
        #endregion
        #region Display Inputs
        if (false)
        {
            Debug.Log("Move: " + move);
            Debug.Log("Jump: " + jump + " Jump Pressed: " + jumpPressed + " Jump Released: " + jumpReleased);
            Debug.Log("Attack: " + attack + " Attack Pressed: " + attackPressed + " Attack Released: " + attackReleased);
            Debug.Log("Dash: " + dash + " Dash Pressed: " + dashPressed + " Dash Released: " + dashReleased);
            Debug.Log("Menu: " + menu + " Menu Pressed: " + menuPressed + " Menu Released: " + menuReleased);
            Debug.Log("Item: " + item + " Item Pressed: " + itemPressed + " Item Released: " + itemReleased);
            Debug.Log("Confirm: " + confirm + " Confirm Pressed: " + confirmPressed + " Confirm Released: " + confirmReleased);
            Debug.Log("Cancel: " + cancel + " Cancel Pressed: " + cancelPressed + " Cancel Released: " + cancelReleased);
        }
        #endregion
    }

    private void FixedUpdate()
    {
        velocity = collision.GlobalToLocal(collision.externalVelocity);
        grounded = collision.detectionDown;


        //Walk

        float targetAcceration = 0;
        float targetSpeed = 0;

        if (move.x == 0)
        {
            targetSpeed = 0;
            targetAcceration = walkDeceleration;
        }
        else
        {
            targetSpeed = walkSpeed * Mathf.Sign(move.x);

            if (move.x * velocity.x > 0)
            {
                targetAcceration = walkAcceleration;
            }
            else
            {
                if (grounded)
                {
                    targetAcceration = walkTurnAcceleration;
                }
                else
                {
                    targetAcceration = airTurnAcceleration;
                }
            }
        }

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, targetAcceration * Time.fixedDeltaTime);




        //Jump
        float jumpForce = jumpStrong;
        

        if (jumpReleased && jumping)
        {
            jumping = false;
            gravity = fallStrong;
            if (velocity.y > 0)
            {
                velocity.y *= jumpReduction;
            }
        }

        if (grounded)
        {
            if (jumpPressed)
            {
                jumping = true;
                gravity = fallWeak;
                velocity.y = jumpForce;
            }
            else
            {
                jumping = false;
                gravity = fallStrong;
            }
        }
        else
        {
            velocity.y = Mathf.MoveTowards(velocity.y, -fallSpeed, gravity * Time.fixedDeltaTime);
        }

        Debug.Log("Jumping: " + jumping);

        collision.externalVelocity = collision.LocalToGlobal(velocity);
        #region Reset Inputs
        jumpPressed = false;
        jumpReleased = false;
        attackPressed = false;
        attackReleased = false;
        dashPressed = false;
        dashReleased = false;
        menuPressed = false;
        menuReleased = false;
        itemPressed = false;
        itemReleased = false;
        confirmPressed = false;
        confirmReleased = false;
        cancelPressed = false;
        cancelReleased = false;
        #endregion
    }
}
