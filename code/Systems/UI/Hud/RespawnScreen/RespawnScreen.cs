﻿
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Threading.Tasks;

namespace Conquest
{
	public enum TransitionState
	{
		ToOverview,
		FromOverview,
		None
	}

	[UseTemplate( "systems/ui/hud/respawnscreen/respawnscreen.html" )]
	public class RespawnScreen : Panel
	{
		private static TransitionState _State = TransitionState.None;
		public static TransitionState State
		{
			get
			{
				if ( TransitionProgress == 1f ) _State = TransitionState.None;

				return _State;
			}
			set
			{
				_State = value;
				TimeSinceStateChanged = 0;
			}
		}

		// @TODO: Move to ExtensionMethods or something
		protected static float EaseOutCirc( float x ) => MathF.Sqrt( 1 - MathF.Pow( x - 1, 2 ) );

		public static float TransitionProgress => MathX.Clamp( TimeSinceStateChanged, 0, TransitionTime ) / TransitionTime;
		public static float EasedTransitionProgress => EaseOutCirc( TransitionProgress );
		public static float TransitionTime => 0.6f; // Takes x seconds for transition
		public static Vector3 OverviewPosition => new Vector3( -186.83f, -805.75f, 5024.03f ); // @TODO: Map entity
		public static Angles OverviewAngles => new Angles( 90, 90, 0 ); // @TODO: Map entity

		public static CameraSetup CameraSetup = new();
		public static TimeSince TimeSinceStateChanged = -1;

		// @ref
		public Button DeployButton { get; set; }
		public Label GameName { get; set; }
		public Label LoadoutPanel { get; set; }
		// -

		public static Vector3 StartingCameraPosition { get; protected set; }
		public static Rotation StartingCameraRotation { get; protected set; }

		public RespawnScreen()
		{
			StartingCameraPosition = Game.LastCameraSnapshot.Pos;
			StartingCameraRotation = Game.LastCameraSnapshot.Rot;
			State = TransitionState.ToOverview;

			CameraSetup.FieldOfView = 90;
			CameraSetup.ZNear = 10;
			CameraSetup.ZFar = 80000;
		}

		public static Vector3 Position => GetStartPos().LerpTo( GetTargetPos(), TransitionProgress );
		public static Rotation Rotation => Rotation.Lerp( GetStartRotation(), GetTargetRotation(), TransitionProgress );

		protected static Vector3 GetTargetPos()
		{
			return State switch
			{
				TransitionState.ToOverview => OverviewPosition,
				TransitionState.FromOverview => Local.Pawn.EyePos,
				_ => Vector3.Zero
			};
		}

		protected static Rotation GetTargetRotation()
		{
			return State switch
			{
				TransitionState.ToOverview => OverviewAngles.ToRotation(),
				TransitionState.FromOverview => Local.Pawn.EyeRot,
				_ => Rotation.Identity
			};
		}

		protected static Vector3 GetStartPos()
		{
			return State switch
			{
				TransitionState.ToOverview => StartingCameraPosition,
				TransitionState.FromOverview => OverviewPosition,
				_ => Vector3.Zero
			};
		}

		protected static Rotation GetStartRotation()
		{
			return State switch
			{
				TransitionState.ToOverview => StartingCameraRotation,
				TransitionState.FromOverview => OverviewAngles.ToRotation(),
				_ => Rotation.Identity
			};
		}

		public override void OnDeleted()
		{
			State = TransitionState.FromOverview;

			base.OnDeleted();
		}

		public void Deploy()
		{
			Host.AssertClient();
			Game.DeployCommand();
		}
	}
}
