# InstanceLocator
A service locator for Unity.

#### Features:
* Register and locate instances of any type for GameObjects, for scenes, or globally.
```
InstanceLocator.Global.Register(this);

InstanceLocator.ForScene(gameObject.scene).Get(out PlayerShip playerShip)

InstanceLocator.For(playerShip.gameObject).TryGet(out ShipMover ShipMover)
```
* Events for instance registration and unregistration.
```
InstanceLocator.Global.AddOnRegisteredListener<PlayerShip>(HandlePlayerShipRegistered);
```
* Supports single or multiple instance registration.
```
InstanceLocator.Global.Register(this, InstanceType.Multiple);

InstanceLocator.ForScene(gameObject.scene).TryGetAll(out IEnumerable<PlayerShip> playerShips)
```

#### Usage:
While no set up is necessary for registering instances globally or for scenes, when registering instances for a GameObject an InstanceLocator component must be added to the GameObject or one of its parents, e.g. the root of a prefab.
