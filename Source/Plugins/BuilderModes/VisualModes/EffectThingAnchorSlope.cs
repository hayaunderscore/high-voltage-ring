#region === Copyright (c) 2010 Pascal van der Heiden ===

using System.Collections.Generic;
using System.Linq;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal class EffectThingAnchorSlope : SectorEffect
	{
		public struct Anchor
		{
			public Thing thing;
			public Vector2D snappedPosition;
		}

		// Anchors used to create this effect
		private List<Anchor> anchors;
		
		// Floor or ceiling?
		private bool slopefloor;
		
		// Constructor
		public EffectThingAnchorSlope(SectorData data, List<Anchor> anchors, bool floor) : base(data)
		{
			this.anchors = anchors;
			slopefloor = floor;
			
			// New effect added: This sector needs an update!
			if(data.Mode.VisualSectorExists(data.Sector))
			{
				BaseVisualSector vs = (BaseVisualSector)data.Mode.GetVisualSector(data.Sector);
				vs.UpdateSectorGeometry(true);
			}
		}
		
		// This makes sure we are updated with the source linedef information
		public override void Update()
		{
			// Create vertices in clockwise order
			Vector3D[] verts = anchors
				.Select(anchor => {
					Vector3D position = anchor.snappedPosition;
					position.z = anchor.thing.Position.z;

					if (anchor.thing.Sector != null)
						position.z += anchor.thing.Sector.FloorHeight;
					
					return position;
				})
				.ToArray();
			
			// Make new plane
			if(slopefloor)
				data.Floor.plane = new Plane(verts[0], verts[1], verts[2], true);
			else
				data.Ceiling.plane = new Plane(verts[0], verts[2], verts[1], false);
		}
	}
}
