# Changelog

## 1.0.0

- Removed duplicate core files and game-specific views, controllers, world bars, and generated code.
- Removed undeclared singleton, EventBus, Addressables, UniTask, DOTween, and game-domain dependencies.
- Added one explicit layered view stack with cover, resume, refresh, exclusive, and back semantics.
- Added replaceable view factory and prefab catalog boundaries.
- Added deterministic lifecycle cleanup and one manager lifecycle test.
