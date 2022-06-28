
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Varneon.BlenderFBXImporter
{
    internal static class DragAndDropHandler
    {
        internal enum DragAndDropState
        {
            None,
            Valid,
            Invalid
        }

        internal static Action HandleFileDragAndDrop(DragAndDropState dragAndDropState, Func<string, bool> addPaths)
        {
            return () =>
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                        if (dragAndDropState == DragAndDropState.None)
                        {
                            dragAndDropState = DragAndDrop.paths.Where(c => c.ToLower().EndsWith(".fbx")).Count() > 0 ? DragAndDropState.Valid : DragAndDropState.Invalid;
                        }
                        else
                        {
                            DragAndDrop.visualMode = dragAndDropState == DragAndDropState.Valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                        }
                        Event.current.Use();
                        return;
                    case EventType.DragPerform:
                        foreach (string path in DragAndDrop.paths.Where(c => c.ToLower().EndsWith(".fbx")))
                        {
                            addPaths(path);
                        }
                        Event.current.Use();
                        return;
                    case EventType.DragExited:
                        dragAndDropState = DragAndDropState.None;
                        return;
                }
            };
        }
    }
}
