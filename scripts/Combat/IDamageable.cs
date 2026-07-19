namespace RPG.Combat;

// Gemeinsame Schnittstelle fuer alles, das im Kampf Schaden nehmen kann (Player, Enemy).
// DamageType (siehe doc/konzept/Gameplay/Kampfsystem.md: slashing|blunt|mixed) ist optional -
// wer Resistenzen/Schwaechen auswertet (aktuell nur Enemy), macht das selbst.
public interface IDamageable
{
	void TakeDamage(int amount, string? damageType);
}
