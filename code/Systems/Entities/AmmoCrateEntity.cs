using Sandbox;

namespace Conquest
{
	[Library( "conquest_ammocrate" )]
	public partial class AmmoCrateEntity : Prop, IUse
	{
		[AdminCmd( "conquest_debug_ammocrate" )]
		public static void CreateAmmoCrate()
		{
			var caller = ConsoleSystem.Caller.Pawn;
			var entity = new AmmoCrateEntity();

			var tr = Trace.Ray( caller.EyePos, caller.EyeRot.Forward * 1000f )
				.WorldAndEntities()
				.Ignore( caller )
				.Run();

			entity.Position = tr.EndPos;
		}

		[Net] public TimeSince LastUsedTime { get; set; }

		public int AmountToGive => 30;
		public float UseCooldown => 1;

		public override void Spawn()
		{
			base.Spawn();

			Health = 100;
			Transmit = TransmitType.Default;

			SetModel( "models/citizen_props/crate01.vmdl" );
		}

		public bool OnUse( Entity user )
		{
			if ( user is Player player )
			{
				player.GiveAll( 120 );
				ClientUsed( To.Single( player.Client ) );

				LastUsedTime = 0;
			}

			return false;
		}

		public bool IsUsable( Entity user )
		{
			return LastUsedTime >= UseCooldown;
		}

		[ClientRpc]
		protected void ClientUsed()
		{
			Log.Info( "testing" );
			KillFeedPanel.Current?.AddMessage( "Ammo replenished" );
		}

		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );
		}
	}
}
