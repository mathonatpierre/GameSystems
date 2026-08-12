namespace GameSystems.Abilities
{
    public interface IAbilityLockService
    {
        bool IsAbilityLocked { get; }
        bool SimulateMotorWhileLocked { get; }
        void BeginAbilityLock(bool keepSimulatingMotor);
        void EndAbilityLock();
    }
}
