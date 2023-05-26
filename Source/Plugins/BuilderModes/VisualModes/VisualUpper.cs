
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal sealed class VisualUpper : BaseVisualGeometrySidedef
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		#endregion

		#region ================== Properties

		#endregion

		#region ================== Constructor / Setup

		// Constructor
		public VisualUpper(BaseVisualMode mode, VisualSector vs, Sidedef s) : base(mode, vs, s)
		{
			//mxd
			geometrytype = VisualGeometryType.WALL_UPPER;
			partname = "top";
			
			// We have no destructor
			GC.SuppressFinalize(this);
		}

		// This builds the geometry. Returns false when no geometry created.
		public override bool Setup()
		{
			Vector2D vl, vr;
			
			// Left and right vertices for this sidedef
			if(Sidedef.IsFront)
			{
				vl = new Vector2D(Sidedef.Line.Start.Position.x, Sidedef.Line.Start.Position.y);
				vr = new Vector2D(Sidedef.Line.End.Position.x, Sidedef.Line.End.Position.y);
			}
			else
			{
				vl = new Vector2D(Sidedef.Line.End.Position.x, Sidedef.Line.End.Position.y);
				vr = new Vector2D(Sidedef.Line.Start.Position.x, Sidedef.Line.Start.Position.y);
			}
			
			// Load sector data
			SectorData sd = Sector.GetSectorData();
			SectorData osd = mode.GetSectorData(Sidedef.Other.Sector);
			if(!osd.Updated) osd.Update();

			//mxd
			double vlzc = sd.Ceiling.plane.GetZ(vl);
			double vrzc = sd.Ceiling.plane.GetZ(vr);

			//mxd. Side is visible when our sector's ceiling is higher than the other's at any vertex
			if(!(vlzc > osd.Ceiling.plane.GetZ(vl) || vrzc > osd.Ceiling.plane.GetZ(vr)))
			{
				base.SetVertices(null);
				return false;
			}

			//mxd. Apply sky hack?
			UpdateSkyRenderFlag();

			//mxd. lightfog flag support
			int lightvalue;
			bool lightabsolute;
			GetLightValue(out lightvalue, out lightabsolute);

			Vector2D tscale = new Vector2D(Sidedef.Fields.GetValue("scalex_top", 1.0),
										   Sidedef.Fields.GetValue("scaley_top", 1.0));
            Vector2D tscaleAbs = new Vector2D(Math.Abs(tscale.x), Math.Abs(tscale.y));
            Vector2D toffset = new Vector2D(Sidedef.Fields.GetValue("offsetx_top", 0.0),
											Sidedef.Fields.GetValue("offsety_top", 0.0));
			
			// Texture given?
			if((Sidedef.LongHighTexture != MapSet.EmptyLongName))
			{
				// Load texture
				base.Texture = General.Map.Data.GetTextureImage(Sidedef.LongHighTexture);
				if(base.Texture == null || base.Texture is UnknownImage)
				{
					base.Texture = General.Map.Data.UnknownTexture3D;
					setuponloadedtexture = Sidedef.LongHighTexture;
				}
				else if (!base.Texture.IsImageLoaded)
                {
					setuponloadedtexture = Sidedef.LongHighTexture;
				}
			}
			else
			{
				// Use missing texture
				base.Texture = General.Map.Data.MissingTexture3D;
				setuponloadedtexture = 0;
			}

			// Get texture scaled size. Round up, because that's apparently what GZDoom does
			Vector2D tsz = new Vector2D(Math.Ceiling(base.Texture.ScaledWidth / tscale.x), Math.Ceiling(base.Texture.ScaledHeight / tscale.y));

			// Get texture offsets
			Vector2D tof = new Vector2D(Sidedef.OffsetX, Sidedef.OffsetY);

			tof = tof + toffset;

			// biwa. Also take the ForceWorldPanning MAPINFO entry into account
			if (General.Map.Config.ScaledTextureOffsets && (!base.Texture.WorldPanning && !General.Map.Data.MapInfo.ForceWorldPanning))
			{
				tof = tof / tscaleAbs;
				tof = tof * base.Texture.Scale;

				// If the texture gets replaced with a "hires" texture it adds more fuckery
				if (base.Texture is HiResImage)
					tof *= tscaleAbs;

				// Round up, since that's apparently what GZDoom does. Not sure if this is the right place or if it also has to be done earlier
				tof = new Vector2D(Math.Ceiling(tof.x), Math.Ceiling(tof.y));
			}

			// Determine texture coordinates plane as they would be in normal circumstances.
			// We can then use this plane to find any texture coordinate we need.
			// The logic here is the same as in the original VisualMiddleSingle (except that
			// the values are stored in a TexturePlane)
			// NOTE: I use a small bias for the floor height, because if the difference in
			// height is 0 then the TexturePlane doesn't work!
			Vector3D vlt, vlb, vrt, vrb;
			Vector2D tlt, tlb, trt, trb;
			double ceilbias = (Sidedef.Other.Sector.CeilHeight == Sidedef.Sector.CeilHeight) ? 1.0 : 0.0;
			double planeceilbias = (Math.Abs(osd.Ceiling.plane.GetZ(vr) - sd.Ceiling.plane.GetZ(vr)) < 0.5) ? 1.0 : 0.0;
			double texturevpeg = 0;

			bool upperunpegged = Sidedef.Line.IsFlagSet(General.Map.Config.UpperUnpeggedFlag);
			bool slopeskew = Sidedef.Line.IsFlagSet(General.Map.Config.SlopeSkewFlag);

			if (!upperunpegged)
			{
				// When lower unpegged is set, the lower texture is bound to the bottom
				if (slopeskew)
				{
					texturevpeg = osd.Ceiling.plane.GetZ(vl) + tsz.y - sd.Ceiling.plane.GetZ(vl);
				}
				else
				{
					texturevpeg = tsz.y - ((float)Sidedef.Sector.CeilHeight - Sidedef.Other.Sector.CeilHeight);
				}
			}
			tlt.x = tlb.x = 0;
			trt.x = trb.x = Sidedef.Line.Length;
			tlt.y = trt.y = texturevpeg;
			tlb.y = trb.y = texturevpeg + (Sidedef.Sector.CeilHeight - (Sidedef.Other.Sector.CeilHeight + ceilbias));

			// Adjust texture y value for sloped walls
			if (!slopeskew)
			{
				// Unskewed
				tlt.y -= sd.Ceiling.plane.GetZ(vl) - Sidedef.Sector.CeilHeight;
				trt.y -= sd.Ceiling.plane.GetZ(vr) - Sidedef.Sector.CeilHeight;
				tlb.y -= osd.Ceiling.plane.GetZ(vl) - Sidedef.Other.Sector.CeilHeight;
				trb.y -= osd.Ceiling.plane.GetZ(vr) - Sidedef.Other.Sector.CeilHeight;
			}
			else if (upperunpegged)
			{
				// Skewed by top
				tlb.y = texturevpeg + sd.Ceiling.plane.GetZ(vl) - osd.Ceiling.plane.GetZ(vl);
				trb.y = texturevpeg + sd.Ceiling.plane.GetZ(vr) - osd.Ceiling.plane.GetZ(vr);
			}
			else
			{
				// Skewed by bottom
				tlb.y = texturevpeg + sd.Ceiling.plane.GetZ(vl) - osd.Ceiling.plane.GetZ(vl);
				trb.y = tlb.y;
				tlt.y = tlb.y - (sd.Ceiling.plane.GetZ(vl) - osd.Ceiling.plane.GetZ(vl));
				trt.y = trb.y - (sd.Ceiling.plane.GetZ(vr) - osd.Ceiling.plane.GetZ(vr));
			}


			if (Math.Abs(trb.y - trt.y) < 0.5f) trb.y = trt.y - 1.0f;

			// Apply texture offset
			tlt += tof;
			tlb += tof;
			trb += tof;
			trt += tof;

			// Transform pixel coordinates to texture coordinates
			tlt /= tsz;
			tlb /= tsz;
			trb /= tsz;
			trt /= tsz;

			// Geometry coordinates
			vlt = new Vector3D(vl.x, vl.y, sd.Ceiling.plane.GetZ(vl));
			vlb = new Vector3D(vl.x, vl.y, osd.Ceiling.plane.GetZ(vl));
			vrb = new Vector3D(vr.x, vr.y, osd.Ceiling.plane.GetZ(vr) + planeceilbias);
			vrt = new Vector3D(vr.x, vr.y, sd.Ceiling.plane.GetZ(vr));

			TexturePlane tp = new TexturePlane();
			tp.tlt = Sidedef.Line.IsFlagSet(General.Map.Config.UpperUnpeggedFlag) ? tlt : tlb;
			tp.trb = trb;
			tp.trt = trt;
			tp.vlt = Sidedef.Line.IsFlagSet(General.Map.Config.UpperUnpeggedFlag) ? vlt : vlb;
			tp.vrb = vrb;
			tp.vrt = vrt;

			// Create initial polygon, which is just a quad between floor and ceiling
			WallPolygon poly = new WallPolygon();
			poly.Add(new Vector3D(vl.x, vl.y, sd.Floor.plane.GetZ(vl)));
			poly.Add(new Vector3D(vl.x, vl.y, vlzc));
			poly.Add(new Vector3D(vr.x, vr.y, vrzc));
			poly.Add(new Vector3D(vr.x, vr.y, sd.Floor.plane.GetZ(vr)));
			
			// Determine initial color
			int lightlevel = lightabsolute ? lightvalue : sd.Ceiling.brightnessbelow + lightvalue;

			//mxd. This calculates light with doom-style wall shading
			PixelColor wallbrightness = PixelColor.FromInt(mode.CalculateBrightness(lightlevel, Sidedef));
			PixelColor wallcolor = PixelColor.Modulate(sd.Ceiling.colorbelow, wallbrightness);
			fogfactor = CalculateFogFactor(lightlevel);
			poly.color = wallcolor.WithAlpha(255).ToInt();
			
			// Cut off the part below the other ceiling
			CropPoly(ref poly, osd.Ceiling.plane, false);
			
			// Cut out pieces that overlap 3D floors in this sector
			List<WallPolygon> polygons = new List<WallPolygon> { poly };
			ClipExtraFloors(polygons, sd.ExtraFloors, false); //mxd

			if(polygons.Count > 0)
			{
				// Keep top and bottom planes for intersection testing
				Vector2D linecenter = Sidedef.Line.GetCenterPoint(); //mxd. Our sector's floor can be higher than the other sector's ceiling!
				top = sd.Ceiling.plane;
				bottom = (osd.Ceiling.plane.GetZ(linecenter) > sd.Floor.plane.GetZ(linecenter) ? osd.Ceiling.plane : sd.Floor.plane);
				
				// Process the polygon and create vertices
				List<WorldVertex> verts = CreatePolygonVertices(polygons, tp, sd, lightvalue, lightabsolute);
				if(verts.Count > 2)
				{
					base.SetVertices(verts);
					return true;
				}
			}
			
			base.SetVertices(null); //mxd
			return false;
		}

		//mxd
		internal void UpdateSkyRenderFlag()
		{
			renderassky = (Sidedef.Other != null && Sidedef.Sector != null && Sidedef.Other.Sector != null
				&& Sidedef.Other.Sector.HasSkyCeiling
				&& (Sidedef.Sector.HasSkyCeiling || Sidedef.HighTexture == "-")
				);
		}
		
		#endregion

		#region ================== Methods

		// Return texture name
		public override string GetTextureName()
		{
			return this.Sidedef.HighTexture;
		}

		// This changes the texture
		protected override void SetTexture(string texturename)
		{
			this.Sidedef.SetTextureHigh(texturename);
			General.Map.Data.UpdateUsedTextures();
			this.Setup();

			//mxd. Other sector also may require updating
			SectorData sd = mode.GetSectorData(Sidedef.Sector);
			if(sd.ExtraFloors.Count > 0)
				((BaseVisualSector)mode.GetVisualSector(Sidedef.Sector)).Rebuild();
		}

		protected override void SetTextureOffsetX(int x)
		{
			Sidedef.Fields.BeforeFieldsChange();
			Sidedef.Fields["offsetx_top"] = new UniValue(UniversalType.Float, (double)x);
		}

		protected override void SetTextureOffsetY(int y)
		{
			Sidedef.Fields.BeforeFieldsChange();
			Sidedef.Fields["offsety_top"] = new UniValue(UniversalType.Float, (double)y);
		}

		protected override void MoveTextureOffset(int offsetx, int offsety)
		{
			Sidedef.Fields.BeforeFieldsChange();
			bool worldpanning = this.Texture.WorldPanning || General.Map.Data.MapInfo.ForceWorldPanning;
			double oldx = Sidedef.Fields.GetValue("offsetx_top", 0.0);
			double oldy = Sidedef.Fields.GetValue("offsety_top", 0.0);
			double scalex = Sidedef.Fields.GetValue("scalex_top", 1.0);
			double scaley = Sidedef.Fields.GetValue("scaley_top", 1.0);
			bool textureloaded = (Texture != null && Texture.IsImageLoaded); //mxd
			double width = textureloaded ? (worldpanning ? this.Texture.ScaledWidth / scalex : this.Texture.Width) : -1; // biwa
			double height = textureloaded ? (worldpanning ? this.Texture.ScaledHeight / scaley : this.Texture.Height) : -1; // biwa

			Sidedef.Fields["offsetx_top"] = new UniValue(UniversalType.Float, GetNewTexutreOffset(oldx, offsetx, width)); //mxd // biwa
			Sidedef.Fields["offsety_top"] = new UniValue(UniversalType.Float, GetNewTexutreOffset(oldy, offsety, height)); //mxd // biwa
		}

		protected override Point GetTextureOffset()
		{
			double oldx = Sidedef.Fields.GetValue("offsetx_top", 0.0);
			double oldy = Sidedef.Fields.GetValue("offsety_top", 0.0);
			return new Point((int)oldx, (int)oldy);
		}

		//mxd
		protected override void ResetTextureScale() 
		{
			Sidedef.Fields.BeforeFieldsChange();
			if(Sidedef.Fields.ContainsKey("scalex_top")) Sidedef.Fields.Remove("scalex_top");
			if(Sidedef.Fields.ContainsKey("scaley_top")) Sidedef.Fields.Remove("scaley_top");
		}

		//mxd
		public override void OnTextureFit(FitTextureOptions options) 
		{
			if(!General.Map.UDMF) return;
			if(!Sidedef.HighRequired() || string.IsNullOrEmpty(Sidedef.HighTexture) || Sidedef.HighTexture == "-" || !Texture.IsImageLoaded) return;
			FitTexture(options);
			Setup();
		}
		
		#endregion
	}
}
