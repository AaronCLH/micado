#light

/// convenient add-ons on top of Autodesk.AutoCAD.Geometry
module BioStream.Micado.Core.Geometry

open Autodesk.AutoCAD.Geometry

let origin2d =
    new Point2d(0.0, 0.0)
    
let origin3d =
    new Point3d(0.0, 0.0, 0.0)

let zUnitVector =
    new Vector3d(0.0, 0.0, 1.0)
    
let xyPlane =
    new Plane(origin3d, zUnitVector)
    
let to3d (point2d : Point2d) =
    new Point3d(xyPlane, point2d)

let to2d (point3d : Point3d) =
    point3d.Convert2d(xyPlane)
    
/// returns the two points, s.t.,
/// the given point is mid-way in between,
/// the distance between the given point and each returned point is the given width,
/// the direction between the given point and each returned point is either the given normal or its reverse                                                           
/// @requires normal is a normal vector
let midSegmentEnds width (normal : Vector2d) (point : Point2d) =
    let vec = normal.MultiplyBy(width)
    point.Subtract(vec), point.Add(vec)
    
/// converts an angle from radians to an integer degree from 0 to 360
let rad2deg (angle : double) =
    int (System.Math.Round (angle * (360.0 / (2.0 * System.Math.PI)))) % 360

/// given an angle in degrees,
/// returns the same angle normalized to an angle from 0 to 360 degrees    
let canonicalDegree angle =
    angle % 360 |> fun (angle) -> if angle < 0 then (angle+360) else angle

/// whether the third angle is in between the first and second angles.
/// the angles are all in degree
let angleWithin a b angle =
    let a = canonicalDegree a
    let b = canonicalDegree b
    if a>b
    then not (b<=angle && angle<a)
    else a<=angle && angle<b