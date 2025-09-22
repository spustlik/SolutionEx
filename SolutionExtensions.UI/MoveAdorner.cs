using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SolutionExtensions.UI
{
    public class MoveAdorner : Adorner
    {
        public MoveAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }
        public bool IsTop { get; private set; }
        public static MoveAdorner Add(UIElement element)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);
            var a = new MoveAdorner(element);
            layer.Add(a);
            return a;
        }
        public static MoveAdorner AddOrUpdate(UIElement element, Point point)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);
            var adorners = layer.GetAdorners(element);
            var a = (adorners?.OfType<MoveAdorner>().FirstOrDefault()) ?? Add(element);
            var rect = new Rect(element.RenderSize);
            a.IsTop = point.Y < rect.Height / 2;
            return a;
        }
        public static MoveAdorner RemoveAll(UIElement element)
        {
            if (element == null)
                return null;
            var layer = AdornerLayer.GetAdornerLayer(element);
            var adorners = layer.GetAdorners(element);
            if (adorners == null)
                return null;
            var result = adorners
                .OfType<MoveAdorner>()
                .Where(x => x.AdornedElement == element)
                .FirstOrDefault();
            foreach (var a in adorners.OfType<MoveAdorner>())
                layer.Remove(a);
            return result;
        }

        protected override void OnRender(DrawingContext ctx)
        {
            var rect = new Rect(AdornedElement.RenderSize);
            var pen = new Pen(new SolidColorBrush(Colors.Red), 3.0);
            if (IsTop)
                DrawInsertLine(ctx, pen, rect.TopLeft, rect.TopRight);
            else
                DrawInsertLine(ctx, pen, rect.BottomLeft, rect.BottomRight);
        }

        private void DrawInsertLine(DrawingContext ctx, Pen pen, Point ps, Point pe)
        {
            ctx.DrawLine(pen, ps, pe);
        }
        private void DrawInsertArrow(DrawingContext ctx, Pen pen, Point ps, Point pe)
        { 
            Point add(Point p, double x, double y) => Point.Add(p, new Vector(x, y));
            var arrow = new Size(10, 6);
            var ofs = arrow.Width;
            var pA = add(ps, ofs, 0);
            var pB = add(pe, -ofs, 0);
            ctx.DrawLine(pen, pA, pB);
            var pAt = add(pA, -arrow.Width, -arrow.Height);
            var pAb = add(pA, -arrow.Width, arrow.Height);
            if (IsTop)
                ctx.DrawLine(pen, pA, pAt);
            else
                ctx.DrawLine(pen, pA, pAb);
            var pBt = add(pB, arrow.Width, -arrow.Height);
            var pBb = add(pB, arrow.Width, arrow.Height);
            if (IsTop)
                ctx.DrawLine(pen, pB, pBt);
            else
                ctx.DrawLine(pen, pB, pBb);
        }
    }
}
