using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

using JAtt = Newtonsoft.Json.JsonPropertyAttribute;
using Req = Newtonsoft.Json.Required;
using NVal = Newtonsoft.Json.NullValueHandling;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  private void RunScript(string json_str, double ring_b_max, double ring_b_min, double ring_taper_angle, double ring_dia_ext, double ring_dia_int, double buildpos_pitch_angle, Plane buildpos_uvw_leading, Plane buildpos_uvw_trailing, ref object json_out)
  {

    // Import JSON string for ring to object classes
    Project proj = JsonConvert.DeserializeObject<Project>(json_str);
    Ring ring = proj.Ring[0];

    // Update ring parameters
    ring.RingDims.BMax = ring_b_max;
    ring.RingDims.BMin = ring_b_min;
    ring.RingDims.TaperAngle = ring_taper_angle;
    ring.RingDims.DiaExt = ring_dia_ext;
    ring.RingDims.DiaInt = ring_dia_int;
    ring.BuildPos.PitchAngle = buildpos_pitch_angle;
    ring.BuildPos.PlaneUvwLeading = ConvertPlaneUvw(buildpos_uvw_leading);
    ring.BuildPos.PlaneUvwTrailing = ConvertPlaneUvw(buildpos_uvw_trailing);

    // Serialise to json for output
    json_out = JsonConvert.SerializeObject(proj, Formatting.Indented);

  }

  // <Custom additional code> 
  public Plane ConvertPlaneGh(PlaneUvw _plUvw) {
    Point3d[] _pts = new Point3d[3];
    Point3d.TryParse(_plUvw.Origin, out _pts[0]);
    Point3d.TryParse(_plUvw.XAxis, out _pts[1]);
    Point3d.TryParse(_plUvw.YAxis, out _pts[2]);
    Plane _plGh = new Plane(_pts[0], new Vector3d(_pts[1]), new Vector3d(_pts[2]));
    return _plGh;
  }

  public PlaneUvw ConvertPlaneUvw(Plane _plGh) {
    PlaneUvw _plUvw = new PlaneUvw();
    _plUvw.Origin = _plGh.Origin.ToString();
    _plUvw.XAxis = _plGh.XAxis.ToString();
    _plUvw.YAxis = _plGh.YAxis.ToString();
    return _plUvw;
  }

  /// <summary>A schema for precast concrete tunnel linings - incl. rings, segments, and internal components</summary>
  public partial class Project
  {
    [JAtt("job_number", Required = Req.Always)]   public int JobNumber { get; set; }        // 8-digit job number for project (hyphen omitted)
    [JAtt("project_name", Required = Req.Always)] public string ProjectName { get; set; }   // Name of project
    [JAtt("ring", Required = Req.Always)]         public Ring[] Ring { get; set; }          // List of precast concrete tunnel lining rings considered in the current project
  }

  /// <summary>A precast concrete tunnel lining ring</summary>
  public partial class Ring
  {
    [JAtt("build_pos", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]   public BuildPos BuildPos { get; set; }    // Definition of ringbuild positions for a given segment ring
    [JAtt("id", Required = Req.Always)]                                                 public int Id { get; set; }               // Unique numeric identifier for ring
    [JAtt("name", Required = Req.Always)]                                               public string Name { get; set; }          // User-defined name for ring
    [JAtt("ring_dims", Required = Req.Always)]                                          public RingDims RingDims { get; set; }    // Object definition for ring dimensions
    [JAtt("ring_type", Required = Req.Always)]                                          public string RingType { get; set; }      // Tunnel ring type
    [JAtt("segment", Required = Req.Always)]                                            public Segment[] Segment { get; set; }    // Collection of segments assigned to this tunnel ring
    [JAtt("tags", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]        public string[] Tags { get; set; }        // User-defined tags for tunnel ring
  }

  /// <summary>Definition of ringbuild positions for a given segment ring</summary>
  public partial class BuildPos
  {
    [JAtt("allow", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]               public int[] Allow { get; set; }                // List of allowable build positions between 0 (no rotation) and n-1 positions rotated; clockwise
    [JAtt("from_previous", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]       public int? FromPrevious { get; set; }          // Build position number relative to previous ring
    [JAtt("pitch_angle", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]         public double? PitchAngle { get; set; }         // Angular distance or pitch between adjacent theoretical positions
    [JAtt("plane_uvw_leading", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]   public PlaneUvw PlaneUvwLeading { get; set; }   // Coordinate system for leading edge ringbuild plane relative to ring plane
    [JAtt("plane_uvw_trailing", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]  public PlaneUvw PlaneUvwTrailing { get; set; }  // Coordinate system for trailing edge ringbuild plane relative to ring plane
    [JAtt("qty", Required = Req.Always)]                                                        public int Qty { get; set; }                    // No. of theoretical positions possible for the ring, i.e. number of dowels included
    [JAtt("start_angle", Required = Req.Always)]                                                public double StartAngle { get; set; }          // Angle to location of first build position (n=1), measured clockwise from tunnel aximuth; (+Y axis)
  }

  /// <summary>Object definition for ring dimensions; ; A collection of dimensions describing a precast concrete tunnel ring</summary>
  public partial class RingDims
  {
    [JAtt("b_max", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]       public double? BMax { get; set; }       // Maximum width [mm] of tunnel ring (W+T)
    [JAtt("b_min", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]       public double? BMin { get; set; }       // Minimum width [mm] of tunnel ring (W-T)
    [JAtt("dia_ext", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public double? DiaExt { get; set; }     // Diameter [m] at extrados of tunnel lining
    [JAtt("dia_int", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public double? DiaInt { get; set; }     // Diameter [m] at intrados of tunnel lining
    [JAtt("Radius", Required = Req.Always)]                                             public double Radius { get; set; }      // Centroidal radius [m] of tunnel lining
    [JAtt("Taper", Required = Req.Always)]                                              public double Taper { get; set; }       // Nominal taper distance [mm] of tunnel ring
    [JAtt("taper_angle", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)] public double? TaperAngle { get; set; } // Taper angle of tunnel ring
    [JAtt("Thickness", Required = Req.Always)]                                          public double Thickness { get; set; }   // Thickness [mm] of tunnel lining
    [JAtt("Width", Required = Req.Always)]                                              public double Width { get; set; }       // Nominal width [mm] of tunnel ring
  }

  /// <summary>A precast concrete tunnel lining segment</summary>
  public partial class Segment
  {
    [JAtt("component", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]           public SegComponent SegComponent { get; set; }               // Collection of tunnel lining components included within this segment
    [JAtt("id", Required = Req.Always)]                                                         public int Id { get; set; }                                  // Unique numeric identifier for segment
    [JAtt("joint", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]               public Joint[] Joint { get; set; }                           // A joint face located at the perimeter of segment
    [JAtt("name", Required = Req.Always)]                                                       public string Name { get; set; }                             // User-defined name for segment
    [JAtt("plane_uvw", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]           public PlaneUvw PlaneUvw { get; set; }                       // Coordinate system for segment relative to ring
    [JAtt("ring_dims", Required = Req.Always)]                                                  public RingDims RingDims { get; set; }                       // Object definition for ring dimensions
    [JAtt("ringbuild_seq", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]       public int? RingbuildSeq { get; set; }                       // Number identifying order in which this segment is installed during the ring build (e.g.; key will be last)
    [JAtt("seg_dims", Required = Req.Always)]                                                   public SegDims SegDims { get; set; }                         // Object definition for segment dimensions
    [JAtt("seg_type", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]            public string SegType { get; set; }                          // User-defined type for segment (e.g. standard/alternate, key/counterkey/non-key, etc.)
    [JAtt("segmentation_point", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]  public SegmentationPoint[] SegmentationPoint { get; set; }   // Object definition of segmentation points assigned to this tunnel segment
    [JAtt("tags", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]                public string[] Tags { get; set; }                           // User-defined tags for tunnel segment
  }

  /// <summary>Collection of tunnel lining components included within this segment</summary>
  public partial class SegComponent
  {
    [JAtt("anchor", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]          public Anchor[] Anchor { get; set; }                 // Collection of anchor points located within segment
    [JAtt("bolt", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]            public Bolt[] Bolt { get; set; }                     // Collection of bolts located within segment
    [JAtt("dowel", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]           public Dowel[] Dowel { get; set; }                   // Collection of circumferential dowels located within segment
    [JAtt("gasket", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]          public Gasket[] Gasket { get; set; }                 // Collection of gaskets located within segment
    [JAtt("grout_socket", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public GroutSocket[] GroutSocket { get; set; }       // Collection of grout sockets located within segment
    [JAtt("guide_rod", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]       public GuideRod[] GuideRod { get; set; }             // Collection of guide rods located within segment
    [JAtt("handling_pocket", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)] public HandlingPocket[] HandlingPocket { get; set; } // Collection of handling pockets located within segment
    [JAtt("indicator_additional", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)] public IndicatorAdditional[] IndicatorAdditional { get; set; } // Collection of indicator additional located within segment
    [JAtt("indicator", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]       public Indicator[] Indicator { get; set; }           // Collection of indicators located within segment
    [JAtt("label", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]           public Label[] Label { get; set; }                   // Collection of labels located within segment
  }

  /// <summary>Anchor point (or indicative dimple) to allow fixing of internal structures / services to; segment</summary>
  public partial class Anchor
  {
    [JAtt("id", Required = Req.Always)]                                                 public int Id { get; set; }                // Unique numeric identifier for anchor point
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]        public string Name { get; set; }           // User-defined name for anchor point
    [JAtt("obj_geom", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string ObjGeom { get; set; }        // Full path with filename for block containing 3D geometry of anchor object(s); note XY; plane and origin of block file will be aligned to PlaneUvw (insertion point)
    [JAtt("plane_uvw", Required = Req.Always)]                                          public PlaneUvw PlaneUvw { get; set; }     // Coordinate system for anchor point relative to segment
    [JAtt("product", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public string Product { get; set; }        // Name of specified dowel product
    [JAtt("void_geom", Required = Req.Always)]                                          public string VoidGeom { get; set; }       // Full path with filename for block containing 3D geometry of concrete void required for; anchor point; note XY plane and origin of block file will be aligned to PlaneUvw; (insertion point)
  }

  /// <summary>Coordinate system for anchor point relative to segment; ; Relative local coordinate system specified by origin point, X-axis vector, and Y-axis; vector; ; Coordinate system for bolt relative to joint face; ; Coordinate system for dowel relative to joint face; ; Coordinate system for grout socket relative to segment; ; Coordinate system for handling pocket relative to segment; ; Coordinate system for indicator relative to joint face; ; Coordinate system for joint relative to segment; ; Coordinate system for segment relative to ring</summary>
  public partial class PlaneUvw
  {
    [JAtt("origin", Required = Req.Always)] public string Origin { get; set; } // origin point for local coordinate system, in format 'x,y,z'
    [JAtt("x_axis", Required = Req.Always)] public string XAxis { get; set; }  // X-axis vector for local coordinate system, in format 'x,y,z'
    [JAtt("y_axis", Required = Req.Always)] public string YAxis { get; set; }  // Y-axis vector for local coordinate system, in format 'x,y,z'
  }

  /// <summary>Bolted connection between tunnel segments</summary>
  public partial class Bolt
  {
    [JAtt("end", Required = Req.Always)]                                                public string End { get; set; }         // End type of bolted connection at specified position
    [JAtt("id", Required = Req.Always)]                                                 public int Id { get; set; }             // Unique numeric identifier for bolt
    [JAtt("joint_face", Required = Req.Always)]                                         public string JointFace { get; set; }   // Joint face on which bolt is located
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]        public string Name { get; set; }        // User-defined name for bolt
    [JAtt("obj_geom", Required = Req.Always)]                                           public string ObjGeom { get; set; }     // Full path with filename for block containing 3D geometry of bolt objects; note XY plane; and origin of block file will be aligned to PlaneUvw (insertion point)
    [JAtt("plane_uvw", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]   public PlaneUvw PlaneUvw { get; set; }  // Coordinate system for bolt relative to joint face
    [JAtt("product", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public string Product { get; set; }     // Name of specified bolt product
    [JAtt("void_geom", Required = Req.Always)]                                          public string VoidGeom { get; set; }    // Full path with filename for block containing 3D geometry of concrete void required for; bolt; note XY plane and origin of block file will be aligned to PlaneUvw (insertion point)

    // Add method for cloning bolt object
    public Bolt Clone()
    {
      return (Bolt) this.MemberwiseClone();
    }
  }

  /// <summary>Circumferential dowel connecting adjacent tunnel rings</summary>
  public partial class Dowel
  {
    [JAtt("build_pos_num", Required = Req.Always)]                                      public int BuildPosNum { get; set; }     // Build position number for dowel
    [JAtt("cl_radius", Required = Req.Always)]                                          public double ClRadius { get; set; }     // Radius from tunnel centreline to centre of installed dowel, measured in plane of; circumferential joint
    [JAtt("dowel_type", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]  public string DowelType { get; set; }    // Type of dowel object installed in segment ring
    [JAtt("id", Required = Req.Always)]                                                 public int Id { get; set; }              // Unique numeric identifier for dowel
    [JAtt("joint_face", Required = Req.Always)]                                         public string JointFace { get; set; }    // Joint face on which dowel is located
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]        public string Name { get; set; }         // User-defined name for dowel
    [JAtt("obj_geom", Required = Req.Always)]                                           public string ObjGeom { get; set; }      // Full path with filename for block containing 3D geometry of dowel; note XY plane aligned; to joint face plane and object inserted at origin point (0,0).
    [JAtt("plane_uvw", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]   public PlaneUvw PlaneUvw { get; set; }   // Coordinate system for dowel relative to joint face
    [JAtt("preinstalled", Required = Req.Always)]                                       public bool Preinstalled { get; set; }   // Status for whether dowel is installed in this segment (true) or the adjacent segment; (false)
    [JAtt("product", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public string Product { get; set; }      // Name of specified dowel product
    [JAtt("void_geom", Required = Req.Always)]                                          public string VoidGeom { get; set; }     // Full path with filename for block containing 3D geometry of concrete void corresponding; to selected dowel; note XY plane aligned to joint face plane and object inserted at; origin point (0,0).

    // Add method for cloning dowel object
    public Dowel Clone()
    {
      return (Dowel) this.MemberwiseClone();
    }
  }

  /// <summary>Compressible rubberized segment gasket that provides waterproof sealing along segment; joints</summary>
  public partial class Gasket
  {
    [JAtt("cl_dist", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public double? ClDist { get; set; }     // Distance from joint centreline to gasket centreline; note: (+) toward intrados, (-); toward extrados
    [JAtt("id", Required = Req.Always)]                                                 public int Id { get; set; }             // Unique numeric identifier for gasket
    [JAtt("joint_face", Required = Req.Always)]                                         public string JointFace { get; set; }   // Joint face on which gasket is located
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]        public string Name { get; set; }        // User-defined name for gasket
    [JAtt("obj_geom", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string ObjGeom { get; set; }     // Full path with filename for block containing 3D geometry of gasket; note XY plane aligned; to joint face plane and object inserted at origin point (0,0).
    [JAtt("obj_section", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)] public string ObjSection { get; set; }  // Full path with filename for block defining 2D geometry of gasket cross section
    [JAtt("plane_uvw", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]   public PlaneUvw PlaneUvw { get; set; }  // Coordinate system for gasket relative to joint face
    [JAtt("product", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public string Product { get; set; }     // Name of specified gasket product
    [JAtt("void_geom", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]   public string VoidGeom { get; set; }    // Full path with filename for block containing 3D geometry of concrete void corresponding; to gasket recess; note XY plane aligned to joint face plane and object inserted at origin; point (0,0).

    // Add method for cloning gasket object
    public Gasket Clone()
    {
      return (Gasket) this.MemberwiseClone();
    }
  }

  /// <summary>Grouting socket installed in segment to allow secondary grouting</summary>
  public partial class GroutSocket
  {
    [JAtt("id", Required = Req.Always)]                                             public int Id { get; set; }             // Unique numeric identifier for grout socket
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string Name { get; set; }        // User-defined name for grout socket
    [JAtt("obj_geom", Required = Req.Always)]                                       public string ObjGeom { get; set; }     // Full path with filename for block containing 3D geometry of grout socket object(s); note; XY plane and origin of block file will be aligned to PlaneUvw (insertion point)
    [JAtt("plane_uvw", Required = Req.Always)]                                      public PlaneUvw PlaneUvw { get; set; }  // Coordinate system for grout socket relative to segment
    [JAtt("product", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)] public string Product { get; set; }     // Name of specified dowel product
    [JAtt("void_geom", Required = Req.Always)]                                      public string VoidGeom { get; set; }    // Full path with filename for block containing 3D geometry of concrete void required for; grout socket; note XY plane and origin of block file will be aligned to PlaneUvw; (insertion point)
  }

  /// <summary>Guide rod ensuring alignment between adjacent segments on longitudinal joint</summary>
  public partial class GuideRod
  {
    [JAtt("id", Required = Req.Always)]                                             public int Id { get; set; }             // Unique numeric identifier for guide rod
    [JAtt("joint_face", Required = Req.Always)]                                     public string JointFace { get; set; }   // Joint face on which bolt is located
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string Name { get; set; }        // User-defined name for guide rod
    [JAtt("obj_geom", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]     public string ObjGeom { get; set; }     // Full path with filename for block containing 3D geometry of bolt objects; note XY plane; and origin of block file will be aligned to PlaneUvw (insertion point)
    [JAtt("plane_uvw", Required = Req.Always)]                                      public PlaneUvw PlaneUvw { get; set; }  // Coordinate system for bolt relative to joint face
    [JAtt("preinstalled", Required = Req.Always)]                                   public bool Preinstalled { get; set; }  // Status for whether guide rod is installed in this segment (true) or the adjacent segment; (false)
    [JAtt("product", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)] public string Product { get; set; }     // Name of specified guide rod product
    [JAtt("void_geom", Required = Req.Always)]                                      public string VoidGeom { get; set; }    // Full path with filename for block containing 3D geometry of concrete void required for; bolt; note XY plane and origin of block file will be aligned to PlaneUvw (insertion point)
  }

  /// <summary>Pocket in lining to assist handling, e.g. by vacuum erector</summary>
  public partial class HandlingPocket
  {
    [JAtt("id", Required = Req.Always)]                                             public int Id { get; set; }            // Unique numeric identifier for handling pocket
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string Name { get; set; }       // User-defined name for handling pocket
    [JAtt("plane_uvw", Required = Req.Always)]                                      public PlaneUvw PlaneUvw { get; set; } // Coordinate system for handling pocket relative to segment
    [JAtt("void_geom", Required = Req.Always)]                                      public string VoidGeom { get; set; }   // Full path with filename for block containing 3D geometry of concrete void for handling; pocket; note XY plane and origin of block file will be aligned to PlaneUvw (insertion; point)
  }

  /// <summary>Indicator in lining to assist handling, e.g. by vacuum erector</summary>
  public partial class IndicatorAdditional
  {
    [JAtt("id", Required = Req.Always)]                                             public int Id { get; set; }            // Unique numeric identifier for handling pocket
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string Name { get; set; }       // User-defined name for handling pocket
    [JAtt("plane_uvw", Required = Req.Always)]                                      public PlaneUvw PlaneUvw { get; set; } // Coordinate system for handling pocket relative to segment
    [JAtt("void_geom", Required = Req.Always)]                                      public string VoidGeom { get; set; }   // Full path with filename for block containing 3D geometry of concrete void for handling; pocket; note XY plane and origin of block file will be aligned to PlaneUvw (insertion; point)
  }

  /// <summary>Indentation in lining to indicate alignment between adjacent segments, e.g. at locations; of bolts and dowels</summary>
  public partial class Indicator
  {
    [JAtt("id", Required = Req.Always)]                                             public int Id { get; set; }            // Unique numeric identifier for indicator
    [JAtt("joint_face", Required = Req.Always)]                                     public string JointFace { get; set; }  // Joint face based on which the indicator is located
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string Name { get; set; }       // User-defined name for indicator
    [JAtt("plane_uvw", Required = Req.Always)]                                      public PlaneUvw PlaneUvw { get; set; } // Coordinate system for indicator relative to joint face
    [JAtt("void_geom", Required = Req.Always)]                                      public string VoidGeom { get; set; }   // Full path with filename for block containing 3D geometry of concrete void for the; indicator; note XY plane and origin of block file will be aligned to PlaneUvw (insertion; point)
  }

  /// <summary>Indentation in lining to label information on the segment, may also include UUID/RFID tag; if used</summary>
  public partial class Label
  {
    [JAtt("id", Required = Req.Always)]                                                 public int Id { get; set; }            // Unique numeric identifier for label
    [JAtt("joint_face", Required = Req.Always)]                                         public string JointFace { get; set; }  // Joint face based on which the label is located
    [JAtt("name", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]        public string Name { get; set; }       // User-defined name for label
    [JAtt("obj_geom", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public string ObjGeom { get; set; }    // Full path with filename for block containing 3D geometry of objects embedded in label; (e.g. RFID chip); note XY plane and origin of block file will be aligned to PlaneUvw; (insertion point)
    [JAtt("plane_uvw", Required = Req.Always)]                                          public PlaneUvw PlaneUvw { get; set; } // Coordinate system for indicator relative to joint face
    [JAtt("text", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]        public string Text { get; set; }       // Alphanumeric text included in label
    [JAtt("void_geom", Required = Req.Always)]                                          public string VoidGeom { get; set; }   // Full path with filename for block containing 3D geometry of concrete void for the label;; note XY plane and origin of block file will be aligned to PlaneUvw (insertion point)
  }

  /// <summary>A joint face located at the perimeter of segment</summary>
  public partial class Joint
  {
    [JAtt("contact", Required = Req.Always)]                                            public string Contact { get; set; }           // Contact conditions for joint behaviour to adjoining segments
    [JAtt("location", Required = Req.Always)]                                           public string Location { get; set; }          // Segment face defined by this joint object
    [JAtt("plane_uvw", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]   public PlaneUvw PlaneUvw { get; set; }        // Coordinate system for joint relative to segment
    [JAtt("pline_face", Required = Req.Always)]                                         public string PlineFace { get; set; }         // Full path with filename for block containing polyline (lines+arcs) defining typical joint; face geometry extruded along joint centreline; note: +Y towards intrados, -Y towards; extrados, -X within segment, +X outside of segment
    [JAtt("pline_relief_left", Required = Req.Always)]                                  public string PlineReliefLeft { get; set; }   // Full path with filename for block containing polyline (lines+arcs) defining joint relief; geometry extruded along line through theoretical joint corner at left of joint face;; note: +X along joint face, +XY quadrant interior of segment
    [JAtt("pline_relief_right", Required = Req.Always)]                                 public string PlineReliefRight { get; set; }  // Full path with filename for block containing polyline (lines+arcs) defining joint relief; geometry extruded along line through theoretical joint corner at right of joint face;; note: +X along joint face, +XY quadrant interior of segment
  }

  /// <summary>Object definition for segment dimensions; ; A collection of dimensions describing a precast concrete tunnel segment</summary>
  public partial class SegDims
  {
    [JAtt("beta", Required = Req.DisallowNull, NullValueHandling = NVal.Ignore)]    public double? Beta { get; set; }           // Angle [deg] for portion of tunnel ring subtended by this segment
    [JAtt("skew_end", Required = Req.Always)]                                       public double SkewEnd { get; set; }         // Angle [deg] defining joint skew (rotation about radial axis) at end of segment's; subtended angle domain (left edge), measured +cw looking outward from tunnel centre; (perpendicular at 0deg)
    [JAtt("skew_start", Required = Req.Always)]                                     public double SkewStart { get; set; }       // Angle [deg] defining joint skew (rotation about radial axis) at start of segment's; subtended angle domain (right edge), measured +cw looking outward from tunnel centre; (perpendicular at 0deg)
    [JAtt("subtend_end", Required = Req.Always)]                                    public double SubtendEnd { get; set; }      // Azimuth angle [deg] measured +cw from point of min. ring taper to end of the segment's; subtended angle domain (left edge)
    [JAtt("subtend_start", Required = Req.Always)]                                  public double SubtendStart { get; set; }    // Azimuth angle [deg] measured +cw from point of min. ring taper to start of the segment's; subtended angle domain (right edge)
    [JAtt("twist_end", Required = Req.Always)]                                      public double TwistEnd { get; set; }        // Angle [deg] defining joint twist (rotation about longitudinal axis) at start of segment's; subtended angle domain (right edge), measured +cw looking towards leading edge (no; rotation at 0deg)
    [JAtt("twist_start", Required = Req.Always)]                                    public double TwistStart { get; set; }      // Angle [deg] defining joint twist (rotation about longitudinal axis) at start of segment's; subtended angle domain (right edge), measured +cw looking towards leading edge (no; rotation at 0deg)
  }

  /// <summary>Geometric control point for theoretical segment extents in global/local coordinate; systems.</summary>
  public partial class SegmentationPoint
  {
    [JAtt("pos_circ", Required = Req.Always)] public string PosCirc { get; set; }   // Circumferential position - right edge (ccw limit), centreline, or left edge (cw limit)
    [JAtt("pos_long", Required = Req.Always)] public string PosLong { get; set; }   // Longitudinal position - leading edge, mid-plane, or trailing edge
    [JAtt("pos_thru", Required = Req.Always)] public string PosThru { get; set; }   // Through-thickness position - intrados, centroid, or extrados
    [JAtt("r", Required = Req.Always)]        public double R { get; set; }         // r-coordinate [mm] in global polar coordinate system
    [JAtt("t", Required = Req.Always)]        public double T { get; set; }         // t-coordinate [deg] in global polar coordinate system
    [JAtt("u", Required = Req.Always)]        public double U { get; set; }         // u-coordinate [mm] as 'x' in local segment cartesian coordinate system
    [JAtt("v", Required = Req.Always)]        public double V { get; set; }         // v-coordinate [mm] as 'y' in local segment cartesian coordinate system
    [JAtt("w", Required = Req.Always)]        public double W { get; set; }         // w-coordinate [mm] as 'z' in local segment cartesian coordinate system
    [JAtt("x", Required = Req.Always)]        public double X { get; set; }         // X-coordinate [mm] in global cartesian coordinate system
    [JAtt("y", Required = Req.Always)]        public double Y { get; set; }         // Y-coordinate [mm] in global cartesian coordinate system
    [JAtt("z", Required = Req.Always)]        public double Z { get; set; }         // Z-coordinate [mm] in global cartesian coordinate system
  }
  // </Custom additional code> 
}