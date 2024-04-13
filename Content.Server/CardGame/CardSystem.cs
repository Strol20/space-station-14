using Content.Shared.Interaction;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using FastAccessors;
using Content.Shared.Storage.Components;

namespace Content.Server.PlayCard;

public sealed class PlayCardSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("PlayCardSystem");

        SubscribeLocalEvent<PlayCardComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<PlayCardComponent, EntGotRemovedFromContainerMessage>(AfterRemove);
        // SubscribeLocalEvent<PlayCardComponent, AfterInteractUsingEvent>(AfterInteractUsing);
    }

    // Types of interactions
    // * use from hand on floor
    // * use from hand inside container
    // * use from hand on other hand
    // EntityUid uid - Item used to interact on other item. Also equals to args.Used
    // args.Target - Item that was clicked on
    // args.User - Player
    private void AfterInteract(EntityUid itemUsedUid, PlayCardComponent itemUsedComp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!TryComp<PlayCardComponent>(args.Target, out var targetComp))
            return;

        if (!TryComp<HandsComponent>(args.User, out var handsComp))
            return;

        // CreateEntityUninitialized(string protoName)
        // InitializeAndStartEntity(EntityUid)
        // TODO: destroy holder if something fails in the process
        var holderUid = _entityManager.CreateEntityUninitialized("PlayCardHolder");
        _entityManager.InitializeAndStartEntity(holderUid);

        if (!_containerSystem.Remove(itemUsedUid, handsComp.ActiveHand!.Container!))
            return;

        if (!_containerSystem.Insert(holderUid, handsComp.ActiveHand!.Container!))
            return;

        if (!TryComp<ContainerManagerComponent>(holderUid, out var holderComp))
            return;

        if (!_containerSystem.Insert(itemUsedUid, holderComp.Containers["storagebase"], null, true))
            return;

        if (args.Target == null || !_containerSystem.Insert((EntityUid) args.Target, holderComp.Containers["storagebase"], null, true))
            return;

        args.Handled = true;

        // create holder
        // move Used to holder
        // move Target to holder
        // put holder in place of Used
        _sawmill.Info("++++++++++ AfterInteract");
        _sawmill.Info($"target component: {targetComp}");
        _sawmill.Info("---------- AfterInteract");
    }

    // uid = item being removed = args.Entity
    // args.Container = container the item is removed from
    private void AfterRemove(EntityUid uid, PlayCardComponent comp, EntGotRemovedFromContainerMessage args)
    {

        // if (args.Container.Owner != comp.Storage)
        //     return;
        if (!TryComp<PlayCardHolderComponent>(args.Container.Owner, out var holderComp))
            return;


        _sawmill.Info("+++++++++++++++ After Remove");
        _sawmill.Info($"uid: {uid}\nargs.Entity: {args.Entity}\nargs.Container: {args.Container}\nargs: {args}");
    }

    /*private void AfterInteractUsing(EntityUid uid, PlayCardComponent comp, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        _sawmill.Info("++++++++++ AfterInteractUsing");
        _sawmill.Info($"uid: {uid};\nargs: {args}");
        _sawmill.Info("---------- AfterInteractUsing");
    }*/
}
