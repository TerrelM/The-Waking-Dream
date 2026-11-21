using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerCollision collision;
    private InputSystem_Actions inputActions;

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
    //Confirm Actions
    private bool confirm;
    private bool confirmPressed;
    private bool confirmReleased;
    //Cancel Actions
    private bool cancel;
    private bool cancelPressed;
    private bool cancelReleased;




    private Vector2 velocity;
    private float strongFall;
    private float weakFall;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        collision = GetComponent<PlayerCollision>();
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }
    private void Start()
    {
        strongFall = 60;
        weakFall = 45;
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
        move = inputActions.PlayerKeyboard.Move.ReadValue<Vector2>();

        //Get Jump Inputs
        jump = inputActions.PlayerKeyboard.Jump.ReadValue<float>() == 1;
        if (inputActions.PlayerKeyboard.Jump.WasPressedThisDynamicUpdate()) { jumpPressed = true; }
        if (inputActions.PlayerKeyboard.Jump.WasReleasedThisDynamicUpdate()) { jumpReleased = true; }

        //Get Attack
        attack = inputActions.PlayerKeyboard.Attack.ReadValue<float>() == 1;
        if (inputActions.PlayerKeyboard.Attack.WasPressedThisDynamicUpdate()) { attackPressed = true; }
        if (inputActions.PlayerKeyboard.Attack.WasReleasedThisDynamicUpdate()) { attackReleased = true; }

        //Get Dash
        dash = inputActions.PlayerKeyboard.Dash.ReadValue<float>() == 1;
        if (inputActions.PlayerKeyboard.Dash.WasPressedThisDynamicUpdate()) { dashPressed = true; }
        if (inputActions.PlayerKeyboard.Dash.WasReleasedThisDynamicUpdate()) { dashReleased = true; }

        //Get Menu
        menu = inputActions.PlayerKeyboard.Menu.ReadValue<float>() == 1;
        if (inputActions.PlayerKeyboard.Menu.WasPressedThisDynamicUpdate()) { menuPressed = true; }
        if (inputActions.PlayerKeyboard.Menu.WasReleasedThisDynamicUpdate()) { menuReleased = true; }

        //Get Confirm
        confirm = inputActions.PlayerKeyboard.Confirm.ReadValue<float>() == 1;
        if (inputActions.PlayerKeyboard.Confirm.WasPressedThisDynamicUpdate()) { confirmPressed = true; }
        if (inputActions.PlayerKeyboard.Confirm.WasReleasedThisDynamicUpdate()) { confirmReleased = true; }

        //Get Cancel
        cancel = inputActions.PlayerKeyboard.Cancel.ReadValue<float>() == 1;
        if (inputActions.PlayerKeyboard.Cancel.WasPressedThisDynamicUpdate()) { cancelPressed = true; }
        if (inputActions.PlayerKeyboard.Cancel.WasReleasedThisDynamicUpdate()) { cancelReleased = true; }
        #endregion
        #region Display Inputs
        if (false)
        {
            Debug.Log("Move: " + move);
            Debug.Log("Jump: " + jump + " Jump Pressed: " + jumpPressed + " Jump Released: " + jumpReleased);
            Debug.Log("Attack: " + attack + " Attack Pressed: " + attackPressed + " Attack Released: " + attackReleased);
            Debug.Log("Dash: " + dash + " Dash Pressed: " + dashPressed + " Dash Released: " + dashReleased);
            Debug.Log("Menu: " + menu + " Menu Pressed: " + menuPressed + " Menu Released: " + menuReleased);
            Debug.Log("Confirm: " + confirm + " Confirm Pressed: " + confirmPressed + " Confirm Released: " + confirmReleased);
            Debug.Log("Cancel: " + cancel + " Cancel Pressed: " + cancelPressed + " Cancel Released: " + cancelReleased);
        }
        #endregion
    }

    private void FixedUpdate()
    {
        velocity = collision.GlobalToLocal(collision.externalVelocity);






        collision.externalVelocity = collision.LocalToGlobal(velocity);
        jumpPressed = false;
        jumpReleased = false;
        attackPressed = false;
        attackReleased = false;
        dashPressed = false;
        dashReleased = false;
        menuPressed = false;
        menuReleased = false;
        confirmPressed = false;
        confirmReleased = false;
        cancelPressed = false;
        cancelReleased = false;
    }
}
