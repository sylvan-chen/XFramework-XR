using XGame.Core;

public class ProcedureEnterGame : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Log.Debug("[ProcedureEnterGame] Enter Game");

        M.UIManager.OpenPanel(100001);
    }
}
