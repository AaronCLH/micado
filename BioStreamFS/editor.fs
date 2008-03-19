#light 

/// Access to drawing editor (user input + output)
/// Add-on to Autodesk.AutoCAD.EditorInput
module BioStream.Micado.Plugin.Editor

open BioStream.Micado.Plugin
open Autodesk.AutoCAD.ApplicationServices
open Autodesk.AutoCAD.DatabaseServices
open Autodesk.AutoCAD.EditorInput

open Autodesk.AutoCAD.Geometry
open BioStream.Micado.Core

/// returns the active editor
let editor() =
    Application.DocumentManager.MdiActiveDocument.Editor

let private defaultColor = 7 // white
let private defaultHighlight = false // no highlighting
let mutable private color = defaultColor
let mutable private highlight = defaultHighlight

/// 1 Red
/// 2 Yellow
/// 3 Green
/// 4 Cyan
/// 5 Blue
/// 6 Magenta
/// 7 White or Black
let setColor c = color <- c
let setHighlight h = highlight <- h

let resetColor () = color <- defaultColor
let resetHighlight () = highlight <- defaultHighlight

/// draw the given points in white, without highlighting
let drawVector (pointA : Point3d) (pointB : Point3d) =
    editor().DrawVector( pointA, 
                         pointB, 
                         color,
                         highlight )
                          
/// writes the given message to the active command line
let writeLine message =
    editor().WriteMessage(message ^ "\n")
    |> ignore

/// prompts the user to answer yes or not:
/// returns true if 'yes' and false if 'no',
/// the boolean parameter is the default similarly coded yes/no value
let promptYesOrNo defaultYes message =
    let options = new PromptKeywordOptions(message)
    options.Keywords.Add("yes")
    options.Keywords.Add("no")
    options.AllowArbitraryInput <- true
    let prompt =
        try
            editor().GetKeywords(options)
        with _ -> null
    let promptPartial x =
        prompt.StringResult.StartsWith(x)
    let promptNot x y =
        prompt = null || (not (promptPartial x) && not (promptPartial y))
    match defaultYes with
    | true -> promptNot "n" "N"
    | false -> promptNot "y" "Y"

/// prompts the user to select an entity
/// returns a tuple of the selected entity and the picked point if the user complies
let promptSelectEntityAndPoint message =
    let promptForEntity =
        try
            editor().GetEntity(new PromptEntityOptions(message))
        with _ -> null
    let ifValid (res : PromptEntityResult) =
        if res = null
           || res.Status = PromptStatus.Error || res.ObjectId.IsNull || not res.ObjectId.IsValid
        then
           writeLine "You did not select an entity.";
           None
        else
        if res.Status = PromptStatus.Cancel
        then None
        else Some (Database.readEntityFromId res.ObjectId, res.PickedPoint)
    promptForEntity |> ifValid
    
/// prompts the user to select an entity
/// returns the selected entity if user complies
let promptSelectEntity message =
    promptSelectEntityAndPoint message
 |> Option.map (function | (entity, point) -> entity)

/// returns the entity as polyline if it's possible
let justPolyline (entity : Entity) =
    match entity with
    | :? Polyline as polyline -> Some polyline
    | _ -> editor().WriteMessage("Selected entity is not a polyline.")
           None

/// prompts the user to select a polyline
/// returns a tuple of the selected polyline and the picked point if the user complies
let promptSelectPolylineAndPoint message =
    promptSelectEntityAndPoint message
 |> Option.bind (function | (entity, point) -> justPolyline entity |> Option.map (fun poly -> (poly, point)))
 
/// prompts the user to select a polyline
/// returns the selected polyline if the user complies
let promptSelectPolyline message =
    promptSelectEntity message |> Option.bind justPolyline

/// converts the polyline to a flow segment if possible
let justFlowSegment (polyline : Polyline) =
    Flow.from_polyline polyline
    |> function
       | None -> writeLine "The selected polyline could not be converted to a flow segment."
                 None
       | s -> s
                    
/// prompts the user to select a flow segment
/// returns the selected segment and the picked point if the user complies
let promptSelectFlowSegmentAndPoint message =
    promptSelectPolylineAndPoint message
 |> Option.bind (function | (polyline, point) -> justFlowSegment polyline |> Option.map (fun flow -> (flow, point)))

/// prompts the user to select a flow segment
/// returns the selected segment if the user complies
let promptSelectFlowSegment message =
    promptSelectPolyline message
 |> Option.bind justFlowSegment
 
/// prompts the user to select a point
/// returns the selected point if the user complies
let promptPoint message =
    let promptForPoint =
        try
            editor().GetPoint(new PromptPointOptions(message))
        with _ -> null
    let pointIfValid (res : PromptPointResult) =
        if res = null
           || res.Status = PromptStatus.Error
        then
           writeLine "You did not select a point.";
           None
        else
        if res.Status = PromptStatus.Cancel
        then None
        else Some res.Value
    promptForPoint |> pointIfValid
    