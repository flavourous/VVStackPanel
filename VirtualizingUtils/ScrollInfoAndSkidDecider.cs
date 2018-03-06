using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualizingUtils
{
    public interface IProvideScrollInfo
    {
        double averageHeight { get; }
        int first { get; }
        double lastIntersection { get; }
        double lastOffset { get; }
        int last { get; }
        double viewportItemsHeight { get; }
        int nItems { get; }

        bool CanHorizontallyScroll { get; set; }
        bool CanVerticallyScroll { get; set; }
        double ExtentHeight { get; }
        double ExtentWidth { get; }
        double HorizontalOffset { get; }
        double LargeScrollAmount { get; set; }
        double LargeScrollFactor { get; set; }
        double SmallScrollAmount { get; set; }
        double VerticalOffset { get; }
        double ViewportHeight { get; }
        double ViewportWidth { get; }
        double WheelScrollAmount { get; set; }

        void LineDown();
        void LineLeft();
        void LineRight();
        void LineUp();
        void MouseWheelDown();
        void MouseWheelLeft();
        void MouseWheelRight();
        void MouseWheelUp();
        void PageDown();
        void PageLeft();
        void PageRight();
        void PageUp();
        void SetHorizontalOffset(double offset);
        void SetVerticalOffset(double offset);

    }


    // its a strategy especially if you extract interface.
    public class ScrollInfoAndSkidDecider<T> where T : IProvideScrollInfo
    {
        readonly T si;

        public ScrollInfoAndSkidDecider(T si)
        {
            this.si = si;
        }

        // use the big jump or smooth scroll strategy
        public bool IsLarge()
        {
            return Math.Abs(si.VerticalOffset - si.lastOffset) > si.ViewportHeight;
        }

        // When determining where to start laying out from, what to use?
        public double GetLayoutTarget()
        {
            var jump = si.VerticalOffset - si.lastOffset;
            // if our offset is going to over or undershoot, slowly being it back in to line. I call this skidding.
            if (jump < 0.0 && !IsLarge())
            {
                var miss = si.VerticalOffset - si.first * si.averageHeight - si.lastIntersection;
                var njumpsremaining = si.VerticalOffset / Math.Abs(jump);
                if (DoubleUtil.AreClose(0, njumpsremaining)) njumpsremaining = 1;
                var correction = miss / njumpsremaining; // a bit
                return si.VerticalOffset + correction;
            }

            // In a perfect world, exactly what the scrollbar requested.
            return si.VerticalOffset;
        }

        // Update some scroll values
        public void UpdateScroll(ref double extent, ref double offset)
        {
            // make sure the extent we reporting here arrives at the last item based on the current offset.
            extent = si.VerticalOffset - si.lastIntersection + si.viewportItemsHeight + si.averageHeight * (si.nItems - si.last - 1);
        }
    }
}
