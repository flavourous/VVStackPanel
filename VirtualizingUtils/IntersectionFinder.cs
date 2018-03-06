using System;
using System.Diagnostics;

namespace VirtualizingUtils
{
    public static class DoubleUtil
    {
        const double TOLERANCE = 1e-5; // since we're dealing in pixels, this will do
                                       // Like MS, we suffer from FPE when doing lots of addition.
        public static bool AreClose(double d1, double d2)
        {
            return Math.Abs(d1 - d2) < TOLERANCE;
        }
    }

    public static class IntersectionFinder
    {
        public class FindIntersecionAndOffsetArgs
        {
            public double initialValue; // our offset is this
            public double initialIntersection; // which corresponds to this far into the current item
            public double targetValue; // and we want to get to this offset
        }
        public class FindIntersectionTools<T>
        {
            public Func<IDisposable> reset_generator;
            public Func<T> generate_next;
            public Func<T, Action> temporarily_realize_item;
            public Func<T, double> get_desired_height;
            public int total_items;
            public bool forwardDirection;
        }
        // FIXME could use an Enumerator
        class FindIntersecionAndOffsetArgsInternal : FindIntersecionAndOffsetArgs
        {
            public FindIntersecionAndOffsetArgsInternal(FindIntersecionAndOffsetArgs ags)
            {
                initialValue = ags.initialValue;
                initialIntersection = ags.initialIntersection;
                targetValue = ags.targetValue;
            }
            public Func<double?> MeasureNext; // this measures the current item then the next etc
        }

        delegate bool incrementorDelegate(ref int c);
        public static FindIntersectionResult FindIntersection<T>(FindIntersecionAndOffsetArgs args, FindIntersectionTools<T> tools, ref int firstIndex)
        {
            int save_firstIndex = firstIndex, ci = firstIndex;
            IDisposable gen = null;
            incrementorDelegate incrementor = (ref int c) =>
            {
                if (c + 1 >= tools.total_items) return true;
                c++;
                return false; // input c is ok
            };
            Func<bool> is_index_ok = () => ci < tools.total_items;
            Action reset = () =>
            {
                int ofs = tools.forwardDirection ? 1 : -1;
                ci = save_firstIndex - ofs;
                gen = tools.reset_generator();
            };

            double lastHeight = 0.0, distance = 0.0;
            var iargs = new FindIntersecionAndOffsetArgsInternal(args)
            {
                MeasureNext = () =>
                {
                    var incres = incrementor(ref ci);
                    Debug.Assert(is_index_ok(), "Bad Index Reached!");
                    if (incres) return null;
                    var cld = tools.generate_next();
                    var unrealize = tools.temporarily_realize_item(cld);
                    distance += lastHeight = tools.get_desired_height(cld);
                    unrealize();
                    return lastHeight;
                },
            };

            // Adaptor for reverse gear
            if (!tools.forwardDirection)
            {
                is_index_ok = () => ci >= 0;
                incrementor = (ref int c) =>
                {
                    if (c - 1 < 0) return true;
                    c--;
                    return false;
                };

                reset();
                // we need to pretend we're going forward.
                iargs.targetValue = iargs.initialValue - iargs.targetValue;
                iargs.initialValue = 0.0;
                var n = iargs.MeasureNext();
                iargs.initialIntersection = n.HasValue ? n.Value - iargs.initialIntersection : 0.0;
                gen.Dispose();
            }

            // Run the helper
            reset();
            var ret = FindIntersecionAndOffset(iargs);
            gen.Dispose();

            firstIndex = ci;
            // backwards we have a correction to make:
            if (!tools.forwardDirection)
            {
                ret.valueReached = args.initialValue - ret.valueReached;
                ret.intersection = lastHeight - ret.intersection;
            }
            return ret;
        }
        public class FindIntersectionResult { public double intersection, valueReached; }
        static FindIntersectionResult FindIntersecionAndOffset(FindIntersecionAndOffsetArgsInternal args)
        {
            Debug.Assert(args.targetValue >= args.initialValue); // we only understand going forward in this method.

            var realTargetValue = args.targetValue + args.initialIntersection; // we pretend we had zero initialintersection here.
            var currentValue = args.initialValue;

            // Find intersection or hit a wall
            double? cheight = 0.0;
            double lastgood = 0.0;
            do
            {
                lastgood = cheight.Value;
                cheight = args.MeasureNext();
                currentValue += cheight ?? 0.0;
            } while (currentValue < realTargetValue && cheight != null);

            // wall or res - if we hit wall - we put intersection at end.
            return new FindIntersectionResult
            {
                intersection = cheight.HasValue ? cheight.Value - currentValue + realTargetValue : lastgood,
                valueReached = cheight.HasValue ? args.targetValue : currentValue - args.initialIntersection
            };
        }

    }




}
