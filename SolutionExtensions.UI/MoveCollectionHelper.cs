using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SolutionExtensions.UI
{
    public class MoveItemArgs : EventArgs
    {
        public object Item { get; }
        /// <summary>
        /// new index of after Item removed from collection
        /// </summary>
        public int NewIndex { get; } //can be > length, then it means add

        public MoveItemArgs(object source, int index)
        {
            Item = source;
            NewIndex= index;
        }
    }
    public class MoveCollectionHelper
    {
        private readonly UIElement owner;
        private readonly IList list;
        private readonly Type collectionItemType;
        private Point start;
        private FrameworkElement movingElement;
        private FrameworkElement movingOverElement;
        public Type VisualItemType { get; set; } = typeof(ListBoxItem);
        public event EventHandler<MoveItemArgs> MoveCompleted;
        public static MoveCollectionHelper Create<T>(UIElement owner, ObservableCollection<T> collection)
        {
            return new MoveCollectionHelper(owner, collection, typeof(T));
        }
        public MoveCollectionHelper(UIElement owner, IList list, Type itemType)
        {
            this.owner = owner;
            this.list = list;
            collectionItemType = itemType;
        }
        [Conditional("DEBUG")]
        private void log(string s) => Debug.WriteLine(s);
        public void AttachToMouseEvents(UIElement itemElement)
        {
            itemElement.MouseDown += ProcessMouseEvent;
            itemElement.MouseUp += ProcessMouseEvent;
            itemElement.MouseMove += ProcessMouseEvent;
        }

        public void ProcessMouseEvent(object sender, MouseEventArgs e)
        {
            var src = sender as FrameworkElement;
            if (src == null) return;
            try
            {
                ProcessMouseEventSafe(src, e);
            }
            catch (Exception ex)
            {
                log("ERROR:" + ex.Message);
                log(ex.ToString());
            }
        }
        private void ProcessMouseEventSafe(FrameworkElement src, MouseEventArgs e)
        {
            if (e.RoutedEvent == UIElement.MouseDownEvent)
            {
                start = e.GetPosition(owner);
                return;
            }
            //if button up (Move or Up)
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopMoving();
                e.Handled = true;
                return;
            }
            //if not moving
            if (movingElement == null)
            {
                if (GetLengthFromStart(e) < 5)
                    return;
                StartMoving(src);
                e.Handled = true;
                return;
            }
            //  else (moving)
            MoveAdorner.RemoveAll(movingOverElement);
            var hit = VisualTreeHelper.HitTest(owner, e.GetPosition(owner));
            var ele = hit?.VisualHit as FrameworkElement;
            // identify underlaying item
            var item = ele?.DataContext;
            if (!HasItemType(item))
                return;
            movingOverElement = ele.FindAncestor(VisualItemType);
            //log($"Moving over {movingOverElement}, {movingOverElement.DataContext}, {item}");
            if (item != movingElement.DataContext)
            {
                //replace adorners with new item
                MoveAdorner.AddOrUpdate(movingOverElement, e.GetPosition(movingOverElement));
            }
            e.Handled = true;
            return;
        }
        private void StartMoving(FrameworkElement src)
        {
            log($"Starting moving with {GetItemPos(src.DataContext)}");
            //start moving
            movingElement = src;
            movingElement.Opacity = 0.3;
            //capture mouse
            if (Mouse.Captured != movingElement)
                Mouse.Capture(movingElement);
        }

        private void StopMoving()
        {
            //else (btn is up, it can be out of list)
            //  if was moving 
            if (movingElement != null)
            {
                //stop moving
                if (movingOverElement != null)
                {
                    var srcItem = movingElement.DataContext;
                    var dstItem = movingOverElement.DataContext;
                    var a = MoveAdorner.RemoveAll(movingOverElement);
                    if (a == null)
                        log($"WARNING:Adorner not found");
                    movingOverElement = null;
                    MoveItems(srcItem, dstItem, a?.IsTop != true);
                }
                movingElement.Opacity = 1;
                movingElement = null;
            }
            // release capture
            if (Mouse.Captured == movingElement)
                Mouse.Capture(null);
        }
        private void MoveItems(object srcItem, object dstItem, bool moveAfter)
        {
            if (srcItem == null || dstItem == null || srcItem == dstItem)
                return;
            if (!HasItemType(dstItem) || !HasItemType(srcItem))
                return;
            log($"End of moving from {GetItemPos(srcItem)} {(moveAfter ? "after" : "before")} {GetItemPos(dstItem)}");
            list.Remove(srcItem);
            var i = list.IndexOf(dstItem);
            if (moveAfter) i++;
            if (i >= list.Count || i < 0)
            {
                i = list.Count;
                list.Add(srcItem);
            }
            else
                list.Insert(i, srcItem);
            this.MoveCompleted?.Invoke(this, new MoveItemArgs(srcItem, i));
        }
        private int GetItemPos(object item)
        {
            if (item == null) return int.MinValue;
            return list.IndexOf(item);
        }

        private bool HasItemType(object item)
        {
            if (item == null) return false;
            return collectionItemType.IsAssignableFrom(item.GetType());
        }
        private double GetLengthFromStart(MouseEventArgs e)
        {
            var pt = e.GetPosition(owner);
            var vec = new Vector(start.X - pt.X, start.Y - pt.Y);
            return vec.Length;
        }

    }
}
