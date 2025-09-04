using Cysharp.Threading.Tasks;
using XGame.Configs;
using XGame.Core;

public class ProcedurePreload : ProcedureBase
{
    public override void OnEnter(StateMachine<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Preload(fsm).Forget();
    }

    private async UniTaskVoid Preload(StateMachine<ProcedureManager> fsm)
    {
        var table = M.TableManager.GetTable<TableUiPanels>();
        var panelConfig = table.GetConfigById(100001);
        await M.UIManager.LoadPanelAsync(panelConfig.Id, panelConfig.Address, panelConfig.ParentLayer);

        fsm.ChangeState<ProcedureEnterGame>();
    }
}
