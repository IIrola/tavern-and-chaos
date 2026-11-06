using UnityEngine;
using UnityEngine.InputSystem;

namespace TavernChaos
{
    public sealed class DemoInput : System.IDisposable
    {
        private readonly InputActionMap game = new("Tavern");
        private readonly InputActionMap pool = new("Billiards");
        private readonly InputActionMap ui = new("Menus");
        private readonly InputActionMap arm = new("Arm wrestling");
        public readonly InputAction ArmPush,ArmConfirm,ArmExit,ArmPause,ArmMode,ArmWager,ArmEffort,ArmRules;
        public readonly InputAction Move, Look, Punch, Interact, Drop, Run, Reset, Pause;
        public readonly InputAction Point, Scroll, PoolAim, FineAim, PoolPower, PoolShoot, PoolExit, PoolPause, PoolReset, PoolNext;
        public readonly InputAction Accept, Back, Navigate;
        public readonly InputAction PoolConfirm,PoolAlternative,PoolPocket,PoolRules;
        public DemoInput()
        {
            Move=game.AddAction("Move",InputActionType.Value);
            Move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d"); Move.AddBinding("<Gamepad>/leftStick");
            Look=game.AddAction("Look",InputActionType.Value); Look.AddBinding("<Mouse>/delta"); Look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=12,y=12)");
            Punch=Button(game,"Punch","<Mouse>/leftButton","<Gamepad>/rightTrigger"); Interact=Button(game,"Interact","<Keyboard>/e","<Gamepad>/buttonSouth");
            Drop=Button(game,"Drop","<Keyboard>/q","<Gamepad>/buttonEast"); Run=Button(game,"Run","<Keyboard>/leftShift","<Gamepad>/leftStickPress");
            Reset=Button(game,"Reset","<Keyboard>/r","<Gamepad>/select"); Pause=Button(game,"Pause","<Keyboard>/escape","<Gamepad>/start");
            Point=pool.AddAction("Point",InputActionType.Value,"<Mouse>/position"); Scroll=pool.AddAction("PowerWheel",InputActionType.Value,"<Mouse>/scroll/y");
            PoolAim=pool.AddAction("AimStick",InputActionType.Value,"<Gamepad>/rightStick");
            FineAim=pool.AddAction("FineAim",InputActionType.Value); FineAim.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/leftArrow").With("Positive","<Keyboard>/rightArrow");
            PoolPower=pool.AddAction("Power",InputActionType.Value); PoolPower.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/downArrow").With("Positive","<Keyboard>/upArrow"); PoolPower.AddBinding("<Gamepad>/dpad/y");
            PoolShoot=Button(pool,"Shoot","<Mouse>/leftButton","<Gamepad>/rightTrigger"); PoolExit=Button(pool,"Leave","<Keyboard>/e","<Gamepad>/buttonEast");
            PoolPause=Button(pool,"Pause","<Keyboard>/escape","<Gamepad>/start"); PoolReset=Button(pool,"Reset","<Keyboard>/r","<Gamepad>/select"); PoolNext=Button(pool,"Exercise","<Keyboard>/tab","<Gamepad>/rightShoulder");
            PoolConfirm=Button(pool,"TableConfirm","<Keyboard>/enter","<Gamepad>/buttonSouth"); PoolAlternative=Button(pool,"TableAlternative","<Keyboard>/backspace","<Gamepad>/buttonNorth"); PoolPocket=Button(pool,"CalledPocket","<Keyboard>/c","<Gamepad>/leftShoulder");
            PoolRules=Button(pool,"TableRules","<Keyboard>/h","<Gamepad>/buttonWest");
            Accept=Button(ui,"Accept","<Keyboard>/enter","<Gamepad>/buttonSouth"); Accept.AddBinding("<Keyboard>/space");
            Back=Button(ui,"Back","<Keyboard>/escape","<Gamepad>/buttonEast");
            Navigate=ui.AddAction("Navigate",InputActionType.Value); Navigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow"); Navigate.AddBinding("<Gamepad>/dpad"); Navigate.AddBinding("<Gamepad>/leftStick");
            ArmPush=Button(arm,"Push","<Keyboard>/space","<Gamepad>/rightTrigger");
            ArmPush.wantsInitialStateCheck=true;
            ArmConfirm=Button(arm,"Confirm","<Keyboard>/enter","<Gamepad>/buttonSouth");
            ArmExit=Button(arm,"Leave","<Keyboard>/e","<Gamepad>/buttonEast");
            ArmPause=Button(arm,"Pause","<Keyboard>/escape","<Gamepad>/start");
            ArmMode=Button(arm,"Input mode","<Keyboard>/m","<Gamepad>/buttonNorth");
            ArmWager=Button(arm,"Wager","<Keyboard>/tab","<Gamepad>/rightShoulder");
            ArmRules=Button(arm,"Rules","<Keyboard>/h","<Gamepad>/buttonWest");
            ArmEffort=arm.AddAction("Effort",InputActionType.Value);ArmEffort.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/leftArrow").With("Positive","<Keyboard>/rightArrow");ArmEffort.AddBinding("<Gamepad>/dpad/x");
            SetContext(false,false);
        }
        private static InputAction Button(InputActionMap map,string n,string key,string pad) { var a=map.AddAction(n,InputActionType.Button); a.AddBinding(key); a.AddBinding(pad); return a; }
        public void SetContext(bool playing,bool billiards,bool wrestling=false)
        {
            game.Disable(); pool.Disable(); ui.Disable();arm.Disable();
            if(!playing) ui.Enable(); else if(wrestling)arm.Enable(); else if(billiards) pool.Enable(); else game.Enable();
        }
        public void Dispose() { game.Dispose(); pool.Dispose(); ui.Dispose();arm.Dispose(); }
    }
}
