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
			public double closeness;
			public Vertex vertex;

			/// <summary>
			/// Gets the final, computed position of the anchor.
			/// 
			/// May throw a <see cref="System.NullReferenceException"/> if
			/// <code>thing</code> is <code>null</code>.
			/// </summary>
			/// <param name="floor">Whether or not to base the height of the vertex off the floor or ceiling</param>
			/// <returns>The anchor position.</returns>
			public Vector3D GetPosition(bool floor)
			{
				Vector3D position = vertex.Position;
				position.z = thing.Position.z;

				if (thing.Sector != null)
				{
					// FIXME: need to create a configurable special for this,
					// though for now, hardcoding works
					if (thing.IsFlagSet("flip"))
						position.z = thing.Sector.CeilHeight - position.z;
					else
						position.z += thing.Sector.FloorHeight;
				}
				
				return position;

			}
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
			Vector3D[] verts = anchors.Select(anchor => anchor.GetPosition(slopefloor)).ToArray();
			
			// Make new plane
			if(slopefloor)
				data.Floor.plane = new Plane(verts[0], verts[1], verts[2], true);
			else
				data.Ceiling.plane = new Plane(verts[0], verts[2], verts[1], false);
		}
	}
}
