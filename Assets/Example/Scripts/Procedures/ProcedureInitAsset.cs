using Cysharp.Threading.Tasks;
using XGame.Core;

public class ProcedureInitAsset : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        InitAsset(fsm).Forget();
    }

    private async UniTaskVoid InitAsset(StateMachine<ProcedureManager> fsm)
    {
        var result = await M.AssetManager.InitPackageAsync();

        if (result.Succeed)
            fsm.ChangeState<ProcedurePreload>();
        else
            Log.Debug("[ProcedureInitAsset] Init Asset Failed");
    }
}
