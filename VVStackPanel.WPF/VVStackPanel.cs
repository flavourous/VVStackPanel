using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using VirtualizingUtils;

namespace VVStackPanel.WPF
{
    public class VirtualizingVerticalStackPanel : VirtualizingPanel, IProvideScrollInfo
    {
        // helpers
        HeightCache hc;
        ScrollInfoAndSkidDecider<VirtualizingVerticalStackPanel> sds;
        VStackAdapter vsa;
        IntersectionFinder.FindIntersectionTools<DependencyObject> tools;


        UIElementCollection _children;
        ItemsControl _itemsControl;
        IItemContainerGenerator _generator;
        Dictionary<UIElement, Rect> _realizedChildLayout = new Dictionary<UIElement, Rect>();

        #region Virtualizing Panel stuff
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _itemsControl = ItemsControl.GetItemsOwner(this);
            _children = InternalChildren;
            _generator = ItemContainerGenerator;

            hc = new HeightCache();
            sds = new ScrollInfoAndSkidDecider<VirtualizingVerticalStackPanel>(this);
            tools = new IntersectionFinder.FindIntersectionTools<DependencyObject>
            {
                temporarily_realize_item = cldo =>
                {
                    // FIXME this doesnt need to be temporary, we can insert and cleanup later for a bit more speed.
                    var cld = cldo as UIElement;
                    if (!_children.Contains(cld))
                    {
                        base.AddInternalChild(cld);
                        return () => base.RemoveInternalChildRange(_children.Count - 1, 1);
                    }
                    else return delegate { };
                }
            };
        }
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);
            // reset scroll info (what about removal of itemz?)
        }

        // Previous Pass Variables (1.1) 
        #region IProvidePrevious
        double _averageHeight = 1.0;
        public double averageHeight { get { return _averageHeight; } } // <h>
        int _first = 0;
        public int first { get { return _first; } } // i
        double _lastIntersection = 0.0;
        public double lastIntersection { get { return _lastIntersection; } } // di
        double _lastOffset = 0.0;
        public double lastOffset { get { return _lastOffset; } } // f
        int _last = 0;
        public int last { get { return _last; } } // l
        double _viewportItemsHeight = 0.0;
        public double viewportItemsHeight { get { return _viewportItemsHeight; } }// v
        public int nItems { get { return _itemsControl.Items.Count; } }
        #endregion

        protected override Size MeasureOverride(Size availableSize)
        {
            Debug.WriteLine("\n\n");
            if (_itemsControl == null || _itemsControl.Items.Count == 0)
                return availableSize; // dont try it ^

            #region reused or unchanging variables
            // Dependency Injection Args for intersection finder
            
            IntersectionFinder.FindIntersecionAndOffsetArgs fiargs;
            IntersectionFinder.FindIntersectionResult fr;

            vsa.availableWidth = availableSize.Width;

            // set viewport to avail - simple in our method
            ViewportHeight = availableSize.Height;
            ViewportWidth = availableSize.Width;
            #endregion

            // FIXME: This is the description of a Strategy pattern.  Refactor to DI/Factory methods.
            // 1. We are called with updated offset u. Need to build/update layout and reestimate extent.
            //   1.1 We have from previous pass: <h> avgheight, i first index in viewport, di intersection of it, f offset,
            //                                   e extent estimate, l last index in viewport, v height of items in viewport
            //   1.2 Calc some correction to u, called u~, used next in (2), where e disagrees with <h> and v for the given f:
            //       This one happens easily when (2.2) jumps over some items with average height != <h> (so, virtually always)
            #region 1.2 Skidding corrections
            double target_height = sds.GetLayoutTarget();
            #endregion
            // 2. Find first index and intersection at u~, getting next i, di and f. 
            //   2.1 Scanning for small delta, measuring each item in the path.
            #region 2.1 Small Offset
            if (!sds.IsLarge())
            {
                tools.forwardDirection = target_height > lastOffset;
                fiargs = new IntersectionFinder.FindIntersecionAndOffsetArgs
                {
                    initialIntersection = lastIntersection,
                    initialValue = lastOffset,
                    targetValue = target_height
                };
                fr = vsa.FindIntersection(fiargs, ref _first);
                _lastIntersection = fr.intersection;
                _lastOffset = VerticalOffset;
                Debug.Assert(first >= 0 && first < nItems, "Impossible index calulated!");
            }
            #endregion
            //   2.2 Division for large delta, big change in layout.
            #region 2.2 Large offset
            else
            {
                // oh ok we moved more than a viewport - lets not try to trace our steps.
                _first = (int)(target_height / averageHeight);
                // Extent Formula is not simple now, so lets defend overruns!
                if (first >= nItems) _first = nItems - 1; // later on fixy code to fit!.
                _lastIntersection = target_height % averageHeight;
                _lastOffset = VerticalOffset; // remember for next time.
            }
            #endregion
            //   2.3 f is recorded as u, not if/what we scan to (skidding)
            // 3. Find last item that fits in viewport. Getting v, <h> and l.
            #region 3.0 Finding last item in viewport
            // FIXME: using intersectionfinder like these steps below results
            //        in unncessary measurment and computation.
            tools.forwardDirection = true;
            var target = lastOffset + ViewportHeight;
            fiargs = new IntersectionFinder.FindIntersecionAndOffsetArgs
            {
                initialIntersection = lastIntersection,
                initialValue = lastOffset,
                targetValue = target
            };
            _last = first;
            fr = vsa.FindIntersection(fiargs, ref _last);
            #endregion
            //   3.1. If we hit final item before filling viewport, scan backwards from the end of 
            //        last item by viewport to find new first index and intersection. updated i and di.
            #region 3.1 rebounding
            if (!DoubleUtil.AreClose(fr.valueReached, target))
            {
                tools.forwardDirection = false;
                fiargs = new IntersectionFinder.FindIntersecionAndOffsetArgs
                {
                    initialIntersection = fr.intersection,
                    initialValue = fr.valueReached,
                    targetValue = fr.valueReached - ViewportHeight
                };
                _first = last;
                fr = vsa.FindIntersection(fiargs, ref _first);
                _lastIntersection = fr.intersection;
                _lastOffset = VerticalOffset;
            }
            #endregion
            // 4. Save the arrangments for the layouts for each item in viewport we found.
            #region 4.0 layout items in viewport 
            // clear arrangment cache FIXME: Use the width cache!
            _realizedChildLayout.Clear();

            var startPos = _generator.GeneratorPositionFromIndex(first);
            int childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;
            int current = first;

            double currentitemHeight = -lastIntersection;
            /* Generator scope */
            using (_generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                _viewportItemsHeight = 0.0;
                while (current <= last)
                {
                    bool newlyRealized;
                    UIElement child = _generator.GenerateNext(out newlyRealized) as UIElement;
                    if (newlyRealized) // FIXME the firstindex finder might have realized them so dont do the adding below here.
                        _generator.PrepareItemContainer(child);

                    if (!_children.Contains(child))
                    {
                        if (childIndex >= _children.Count) base.AddInternalChild(child);
                        else base.InsertInternalChild(childIndex, child);
                    }
                    //else Debug.Assert(child == _children[childIndex], "Wrong child was generated");

                    //always needed
                    child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                    _realizedChildLayout.Add(child, new Rect(0, currentitemHeight, availableSize.Width, child.DesiredSize.Height));

                    //accumulators.
                    var ch = _realizedChildLayout[child].Height;
                    hc.Push(current, ch, availableSize.Width);
                    currentitemHeight += ch;
                    _viewportItemsHeight += ch;
                    current++;
                    childIndex++;
                }
            }
            #endregion
            // 5. Compute the updated scroll info, getting e and <h>, and possibly a correction to f and u
            #region 5.0 update extent info and <h>
            ExtentWidth = availableSize.Width; // easy?
            _averageHeight = hc.GetAverageHeight(); // used on next pass to help find first index
            sds.UpdateScroll(ref _ExtentHeight, ref _VerticalOffset);
            Debug.WriteLine("Passs: #{4}-{5} {0}/{1}@{2} with <{3}>", VerticalOffset, ExtentHeight, ViewportHeight, averageHeight, first, last);
            Debug.Assert(!double.IsNaN(averageHeight) && !double.IsInfinity(averageHeight));
            Debug.Assert(!double.IsNaN(ExtentHeight) && !double.IsInfinity(ExtentHeight));
            ScrollOwner.InvalidateScrollInfo(); // seems to cause recursion to this method?
            #endregion

            CleanUpItems(first, last); // cleanup old ones.
            return availableSize; // take all avail size.
        }


        public void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                GeneratorPosition childGeneratorPos = new GeneratorPosition(i, 0);
                int itemIndex = _generator.IndexFromGeneratorPosition(childGeneratorPos);
                if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
                {
                    _generator.Remove(childGeneratorPos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_children != null)
                foreach (UIElement child in _children)
                    child.Arrange(_realizedChildLayout[child]);
            return finalSize;
        }
        #endregion

        #region IScrollInfo

        public double SmallScrollAmount { get { return (double)GetValue(SmallScrollAmountProperty); } set { SetValue(SmallScrollAmountProperty, value); } }
        public static readonly DependencyProperty SmallScrollAmountProperty = DependencyProperty.Register("SmallScrollAmount", typeof(double), typeof(VirtualizingVerticalStackPanel), new PropertyMetadata(6.0));

        public double WheelScrollAmount { get { return (double)GetValue(WheelScrollAmountProperty); } set { SetValue(WheelScrollAmountProperty, value); } }
        public static readonly DependencyProperty WheelScrollAmountProperty = DependencyProperty.Register("WheelScrollAmount", typeof(double), typeof(VirtualizingVerticalStackPanel), new PropertyMetadata(6.0));

        public double LargeScrollAmount { get { return (double)GetValue(LargeScrollAmountProperty); } set { SetValue(LargeScrollAmountProperty, value); } }
        public static readonly DependencyProperty LargeScrollAmountProperty = DependencyProperty.Register("LargeScrollAmount", typeof(double), typeof(VirtualizingVerticalStackPanel), new PropertyMetadata(0.0));

        public double LargeScrollFactor { get { return (double)GetValue(LargeScrollFactorProperty); } set { SetValue(LargeScrollFactorProperty, value); } }
        public static readonly DependencyProperty LargeScrollFactorProperty = DependencyProperty.Register("LargeScrollFactor", typeof(double), typeof(VirtualizingVerticalStackPanel), new PropertyMetadata(1.0));

        public void LineUp() { SetVerticalOffset(VerticalOffset - SmallScrollAmount); }
        public void LineDown() { SetVerticalOffset(VerticalOffset + SmallScrollAmount); }
        public void LineLeft() { }
        public void LineRight() { }
        public void PageUp() { SetVerticalOffset(VerticalOffset - LargeScrollAmount + LargeScrollFactor * ViewportHeight); }
        public void PageDown() { SetVerticalOffset(VerticalOffset + LargeScrollAmount + LargeScrollFactor * ViewportHeight); }
        public void PageLeft() { }
        public void PageRight() { }
        public void MouseWheelUp() { SetVerticalOffset(VerticalOffset - WheelScrollAmount); }
        public void MouseWheelDown() { SetVerticalOffset(VerticalOffset + WheelScrollAmount); }
        public void MouseWheelLeft() { }
        public void MouseWheelRight() { }
        public void SetHorizontalOffset(double offset) { }
        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || ViewportHeight >= ExtentHeight)
            {
                offset = 0;
            }
            else
            {
                if (offset + ViewportHeight >= ExtentHeight)
                {
                    offset = ExtentHeight - ViewportHeight;
                }
            }

            _VerticalOffset = offset;
            InvalidateMeasure();
        }
        public Rect MakeVisible(Visual visual, Rect rectangle) { return rectangle; }
        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }
        public double ExtentWidth { get; private set; }
        public double _ExtentHeight;
        public double ExtentHeight { get { return _ExtentHeight; } }
        public double ViewportWidth { get; private set; }
        public double ViewportHeight { get; private set; }
        public double HorizontalOffset { get; private set; }
        public double _VerticalOffset;
        public double VerticalOffset { get { return _VerticalOffset; } }
        public ScrollViewer ScrollOwner { get; set; }
        #endregion
    }
}
