﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Conquest
{
	public partial class Player : BasePlayer
	{
		/// <summary>
		/// The clothing container is what dresses the citizen
		/// </summary>
		public Clothing.Container Clothing = new();

		[Net] public TeamSystem.Team Team { get; set; } = TeamSystem.Team.BLUFOR;

		[Net, Predicted] public ICamera MainCamera { get; set; }
		[Net, Predicted] public bool IsSprinting { get; protected set; }
		[Net, Predicted] public bool IsAiming { get; protected set; }
		[Net, Predicted] public bool IsFreeLooking { get; protected set; }

		public ICamera LastCamera { get; set; }

		protected override void MakeHud()
		{
			Hud = new PlayerHud();
		}

		public Player() : base()
		{
			Inventory = new PlayerInventory( this );
		}

		public Player( Client cl ) : this()
		{
			// Load clothing from client data
			Clothing.LoadFromClient( cl );

			if ( !Host.IsServer )
				return;
		}

		public override void Spawn()
		{
			MainCamera = new FirstPersonCamera();
			LastCamera = MainCamera;

			base.Spawn();
		}

		protected virtual void StripWeapons()
		{
			ClearAmmo();

			// Clear a player's inventory
			Inventory?.DeleteContents();
		}

		protected virtual void GiveLoadout()
		{
			Inventory.Add( new Pistol(), true );
			Inventory.Add( new SMG(), true );
		}

		protected virtual void SoftRespawn()
		{
			StripWeapons();
			GiveLoadout();
			Clothing.DressEntity( this );
			MoveToSpawnpoint( this );
		}

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new WalkController();
			Animator = new PlayerAnimator();

			MainCamera = LastCamera as FirstPersonCamera;
			Camera = MainCamera;

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			base.Respawn();

			SoftRespawn();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			Inventory.DropActive();
			Inventory.DeleteContents();

			BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );

			Controller = null;
			Camera = new SpectateRagdollCamera();

			EnableAllCollisions = false;
			EnableDrawing = false;
		}

		public ICamera GetActiveCamera()
		{
			return MainCamera;
		}

		protected void HandleSharedInput( Client cl )
		{
			var isReloading = ActiveChild is BaseWeapon weapon && weapon.IsReloading;
			IsSprinting = Input.Down( InputButton.Run ) && Velocity.Length > 10;
			IsAiming = !IsSprinting && Input.Down( InputButton.Attack2 ) && !isReloading;
			IsFreeLooking = Input.Down( InputButton.Walk );
		}

		public override void FrameSimulate( Client cl )
		{
			base.FrameSimulate( cl );

			// @TODO: This is fucking awful
			if ( ActiveChild is BaseCarriable weapon && weapon.CrosshairPanel is not null )
			{
				if ( IsAiming && !weapon.CrosshairPanel.HasClass( "aim" ) )
				{
					weapon.CrosshairPanel.AddClass( "aim" );
				}
				else if ( !IsAiming && weapon.CrosshairPanel.HasClass( "aim" ) )
				{
					weapon.CrosshairPanel.RemoveClass( "aim" );
				}

				if ( Velocity.Length > 10 && !weapon.CrosshairPanel.HasClass( "move" ) )
				{
					weapon.CrosshairPanel.AddClass( "move" );
				}
				else if ( Velocity.Length <= 10 && weapon.CrosshairPanel.HasClass( "move" ) )
				{
					weapon.CrosshairPanel.RemoveClass( "move" );
				}

				if ( IsSprinting && !weapon.CrosshairPanel.HasClass( "movefast" ) )
				{
					weapon.CrosshairPanel.AddClass( "movefast" );
				}
				else if ( !IsSprinting && weapon.CrosshairPanel.HasClass( "movefast" ) )
				{
					weapon.CrosshairPanel.RemoveClass( "movefast" );
				}
			}
		}

		public void SwitchToBestWeapon()
		{
			var best = Children.Select( x => x as ICarriable )
				.Where( x => x is not null && x.IsUsable() )
				.OrderByDescending( x => x.BucketWeight )
				.FirstOrDefault();

			if ( best == null ) return;

			ActiveChild = best as BaseCarriable;
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			HandleSharedInput( cl );

			if ( Input.ActiveChild != null )
				ActiveChild = Input.ActiveChild;

			if ( LifeState != LifeState.Alive )
				return;

			Camera = GetActiveCamera();

			IsFreeLooking = Input.Down( InputButton.Walk );

			var controller = GetActiveController();
			if ( controller != null )
				EnableSolidCollisions = !controller.HasTag( "noclip" );

			if ( Input.Pressed( InputButton.Drop ) )
			{
				var dropped = Inventory.DropActive();
				if ( dropped != null )
				{
					if ( dropped.PhysicsGroup != null )
						dropped.PhysicsGroup.Velocity = Velocity + ( EyeRot.Forward + EyeRot.Up ) * 300;

					SwitchToBestWeapon();
				}
			}

			TickPlayerUse();

			SimulateActiveChild( cl, ActiveChild );
		}

		public override PawnController GetActiveController()
		{
			return base.GetActiveController();
		}

		public virtual void Notify( string message, string hex = "#ffffff" )
		{
			// NotificationBox.AddChatEntry( To.Single( Client ), message, hex );
		}
	}
}
