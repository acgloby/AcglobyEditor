using GameFramework.Fsm;
using GameFramework.Procedure;

public class ProcedureTitle : ProcedureBase
{
    protected override void OnEnter(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);

    }

    protected override void OnLeave(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner, bool isShutdown)
    {
        base.OnLeave(procedureOwner, isShutdown);
    }
}
