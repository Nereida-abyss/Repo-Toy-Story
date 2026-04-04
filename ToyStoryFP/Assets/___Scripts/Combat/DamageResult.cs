public struct DamageResult
{
    public static readonly DamageResult None = new DamageResult(false, false, 0);

    public DamageResult(bool wasDamaged, bool wasKilled, int damageApplied)
    {
        WasDamaged = wasDamaged;
        WasKilled = wasKilled;
        DamageApplied = damageApplied;
    }

    public bool WasDamaged { get; }
    public bool WasKilled { get; }
    public int DamageApplied { get; }
}
