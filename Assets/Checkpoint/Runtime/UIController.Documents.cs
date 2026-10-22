using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Checkpoint
{
    public sealed partial class UIController
    {
        public void OpenDocument(string id)
        {
            if (deskStage == null) return;
            if (documentWindows.TryGetValue(id, out var existing)) { existing.BringToFront(); return; }
            var document = shift.Current.documents.First(d => d.id == id);
            shift.OpenDocument(id);
            if (openOrder.Count == 2) CloseDocument(openOrder[0]);
            deskStage.Q("empty-desk")?.RemoveFromHierarchy();
            var window = Box("document-window", deskStage, "document-" + id);
            documentWindows[id] = window; openOrder.Add(id);
            var titlebar = Box("document-handle row", window);
            Text(document.type, "document-type", titlebar);
            Button("×", "document-close", () => CloseDocument(id), titlebar, "close-" + id);
            titlebar.tooltip = "Drag this header to move. ARRANGE restores both documents side by side.";
            var content = Scroll(window, "document-scroll");
            Text(document.issuer.ToUpperInvariant(), "document-issuer", content);
            var number = Box("document-number-row row", content);
            Text(document.number, "document-number", number);
            Text("VERIFIED\nPRINT", "document-seal", number);
            foreach (var f in document.fields)
            {
                var captured = f;
                var button = Button("", "document-field", () => Inspect(captured), content, "field-" + f.id);
                Text(f.label.ToUpperInvariant(), "field-label", button);
                Text(f.value, "field-value", button);
                button.EnableInClassList("field-selected", selectedField == f.id);
                button.tooltip = f.label + ": " + f.value + ". Activate to inspect or compare.";
                fieldButtons[f.id] = button;
            }
            Text(document.footnote, "document-footnote", content);
            Text("◂ CLICK FIELDS TO COMPARE ▸", "document-instruction", content);
            var dock = Box("document-dock row", window);
            Button("DOCK LEFT", "dock-button", () => Dock(id, false), dock);
            Button("DOCK RIGHT", "dock-button", () => Dock(id, true), dock);
            MakeDraggable(titlebar, window);
            ArrangeDocuments(); RefreshStatus();
        }
        void CloseDocument(string id)
        {
            if (!documentWindows.TryGetValue(id, out var panel)) return;
            foreach (var f in shift.Current.documents.First(d => d.id == id).fields) fieldButtons.Remove(f.id);
            panel.RemoveFromHierarchy(); documentWindows.Remove(id); openOrder.Remove(id);
            ArrangeDocuments();
            if (openOrder.Count == 0)
            {
                var empty = Box("desk-empty", deskStage, "empty-desk");
                Text("DOCUMENTS RETURNED TO TRAY", "empty-heading", empty);
                Text("Open any document above to continue.", "small muted centered", empty);
            }
        }
        void ArrangeDocuments()
        {
            if (deskStage == null || float.IsNaN(deskStage.resolvedStyle.width) || deskStage.resolvedStyle.width < 50) return;
            float width = deskStage.resolvedStyle.width;
            float height = deskStage.resolvedStyle.height;
            float cardWidth = Mathf.Min(350, (width - 30) / 2);
            foreach (var id in openOrder)
            {
                var panel = documentWindows[id];
                panel.style.width = cardWidth;
                panel.style.height = Mathf.Max(120, height - 20);
                panel.style.left = 10 + openOrder.IndexOf(id) * (cardWidth + 10);
                panel.style.top = 10;
            }
        }
        void Dock(string id, bool right)
        {
            if (!documentWindows.TryGetValue(id, out var panel)) return;
            panel.style.left = right ? Mathf.Max(0, deskStage.resolvedStyle.width - panel.resolvedStyle.width - 10) : 10;
            panel.style.top = 10; panel.BringToFront();
        }
        void MakeDraggable(VisualElement handle, VisualElement window)
        {
            Vector2 origin = default, position = default;
            int pointer = -1;
            handle.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button != 0 || evt.target is Button) return;
                pointer = evt.pointerId; origin = evt.position;
                position = new Vector2(window.resolvedStyle.left, window.resolvedStyle.top);
                handle.CapturePointer(pointer); window.BringToFront(); evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt => {
                if (pointer != evt.pointerId || !handle.HasPointerCapture(pointer)) return;
                var delta = (Vector2)evt.position - origin;
                window.style.left = Mathf.Clamp(position.x + delta.x, 0, Mathf.Max(0, deskStage.resolvedStyle.width - window.resolvedStyle.width));
                window.style.top = Mathf.Clamp(position.y + delta.y, 0, Mathf.Max(0, deskStage.resolvedStyle.height - window.resolvedStyle.height));
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt => {
                if (pointer != evt.pointerId) return;
                handle.ReleasePointer(pointer); pointer = -1; evt.StopPropagation();
            });
            handle.RegisterCallback<PointerCaptureOutEvent>(_ => pointer = -1);
        }
        void Inspect(DocumentField field)
        {
            string inspection = shift.InspectField(field.id);
            if (!string.IsNullOrEmpty(selectedField) && selectedField != field.id)
            {
                string comparison = shift.CompareFields(selectedField, field.id);
                feedback.text = comparison.StartsWith("No documented") ? inspection + "\n" + comparison : comparison;
                selectedField = null;
            }
            else
            {
                selectedField = selectedField == field.id ? null : field.id;
                feedback.text = inspection + (selectedField == null ? "" : "\nSELECTED / Choose a second field to compare.");
            }
            foreach (var entry in fieldButtons) entry.Value.EnableInClassList("field-selected", entry.Key == selectedField);
            RefreshStatus(); RefreshLeft();
        }
    }
}
