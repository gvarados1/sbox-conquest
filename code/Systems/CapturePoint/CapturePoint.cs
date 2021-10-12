using Conquest.UI;
using Sandbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Conquest
{
	public partial class CapturePointEntity : ModelEntity
	{
		[Net, Category("Capture Point")]
		public string Identity { get; set; }

		[Net, Category( "Capture Point" )]
		public TeamSystem.Team Team { get; set; } = TeamSystem.Team.Unassigned;

		protected static int ArraySize => Enum.GetNames( typeof( TeamSystem.Team ) ).Length - 1;

		[Net, Category( "Capture Point" )]
		public List<int> OccupantCounts { get; set; } = new();

		[Net, Category( "Capture Point")]
		public float Captured { get; set; } = 0;

		// takes 10s to cap
		public float CaptureTime => 10;

		// @Server
		public Dictionary<TeamSystem.Team, HashSet<Player>> Occupants { get; set; } = new();

		public CapturePointEntity()
		{
			if ( Host.IsClient )
			{
				Marker = new CapturePointHudMarker( this );
			}

			if ( Host.IsServer )
			{
				for ( int i = 0; i < ArraySize; i++ )
					OccupantCounts.Add( 0 );

				// Initialize the dictionary's list values.
				foreach ( TeamSystem.Team team in Enum.GetValues( typeof( TeamSystem.Team ) ) )
				{
					if ( team == TeamSystem.Team.Unassigned )
						continue;

					Log.Info( "Creating entry for " + team.ToString() );
					Occupants[team] = new();
				}
			}
		}

		public CapturePointHudMarker Marker { get; set; }

		public override void Spawn()
		{
			base.Spawn();

			// Set the default size
			SetTriggerSize( 128 );

			// Client doesn't need to know about htis
			Transmit = TransmitType.Always;
		}
		public override void ClientSpawn()
		{
			base.ClientSpawn();
		}

		/// <summary>
		/// Set the trigger radius. Default is 16.
		/// </summary>
		public void SetTriggerSize( float radius )
		{
			SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, radius );
			CollisionGroup = CollisionGroup.Trigger;
		}
		
		protected void AddPlayer( Player player )
		{
			// Already in the list!
			if ( Occupants[player.Team].Contains( player ) )
				return;

			Occupants[player.Team].Add( player );
			OccupantCounts[(int)player.Team]++;
		}

		protected void RemovePlayer( Player player )
		{
			if ( !Occupants[player.Team].Contains( player ) )
				return;

			Occupants[player.Team].Remove( player );
			OccupantCounts[(int)player.Team]--;
		}

		public override void StartTouch( Entity other )
		{
			base.StartTouch( other );

			if ( Host.IsServer && other is Player player )
			{
				AddPlayer( player );
			}
		}

		public override void EndTouch( Entity other )
		{
			base.EndTouch( other );

			if ( Host.IsServer && other is Player player )
			{
				RemovePlayer( player );
			}
		}

		[Event.Tick.Server]
		public void Tick()
		{
			if ( Occupants is null || OccupantCounts is null )
				return;

			if ( Occupants.Count == 0 || OccupantCounts.Count == 0 )
				return;


			var lastCount = 0;
			var highest = TeamSystem.Team.Unassigned;
			var contested = false;
			for ( int i = 0; i < OccupantCounts.Count; i++ )
			{
				var team = (TeamSystem.Team)i;
				var count = OccupantCounts[i];

				if ( lastCount > 0 && count > 0 )
				{
					contested = true;
					break;
				}

				if ( count > 0 )
				{
					lastCount = count;
					highest = team;
				}
			}

			// nobody is fighting for this point (which shouldn't really happen)
			if ( highest == TeamSystem.Team.Unassigned )
				return;

			// Don't do anythig while we're contested
			if ( contested )
				return;

			// A team is trying to cap. Let's reverse this shit.
			if ( Team != TeamSystem.Team.Unassigned && highest != Team )
			{
				float attackMultiplier = MathF.Sqrt( lastCount ); // Somewhat random sub-linear scale
				Captured = MathX.Clamp( Captured - Time.Delta * attackMultiplier / CaptureTime, 0, 1 );

				if ( Captured == 0f )
				{
					Team = TeamSystem.Team.Unassigned;
				}
			}
			else
			{
				float attackMultiplier = MathF.Sqrt( lastCount ); // Somewhat random sub-linear scale
				Captured = MathX.Clamp( Captured + Time.Delta * attackMultiplier / CaptureTime, 0, 1 );

				if ( Captured == 1f )
				{
					Team = highest;
				}
				else
				{
					Team = TeamSystem.Team.Unassigned;
				}
			}
		}
	}
}
