using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace VVStackPanel
{
    public static class IntersectionFinderTests
    {
        public static void RunTests()
        {
            //       ↓ initial (27.0 offset, 8.0 intersect)
            // |---|---|---|---|---|
            //              ↑ target (59.0 offset, 2.0 intersect)
            new TestSpec(5, 19.0)
            {
                startindex = 1,
                initialValue = 27,
                initialIntersection = 8,
                targetValue = 59,
                direction = GeneratorDirection.Forward,

                ASSERT_index = 3,
                ASSERT_intersection = 2,
                ASSERT_valuereached = 59
            }.Run();

            //      ↓ initial (22.1 offset, 3.1 intersect)
            // |---|---|---|---|---| 
            //        ↑ target (37.2 offset, 18.2 intersect)
            new TestSpec(5, 19.0)
            {
                startindex = 1,
                initialValue = 22.1,
                initialIntersection = 3.1,
                targetValue = 37.2,
                direction = GeneratorDirection.Forward,

                ASSERT_index = 1,
                ASSERT_intersection = 18.2,
                ASSERT_valuereached = 37.2
            }.Run();

            //      ↓ initial (22.1 offset, 3.1 intersect)
            // |---|---|---|---|---| 
            //    ↑ target (17.1 offset, 17.1 intersect)    
            new TestSpec(5, 19.0)
            {
                startindex = 1,
                initialValue = 22.1,
                initialIntersection = 3.1,
                targetValue = 17.1,
                direction = GeneratorDirection.Backward,

                ASSERT_index = 0,
                ASSERT_intersection = 17.1,
                ASSERT_valuereached = 17.1
            }.Run();

            //           ↓ initial (47.2 offset, 9.2 intersect)
            // |---|---|---|---|---| 
            //          ↑ target (39.1 offset, 1.1 intersect)    
            new TestSpec(5, 19.0)
            {
                startindex = 2,
                initialValue = 47.2,
                initialIntersection = 9.2,
                targetValue = 39.1,
                direction = GeneratorDirection.Backward,

                ASSERT_index = 2,
                ASSERT_intersection = 1.1,
                ASSERT_valuereached = 39.1
            }.Run();

            //           ↓ initial (47.2 offset, 9.2 intersect)
            // |---|---|---|---|---| 
            //↑ target (-4.5 offset, resets to 0.0 intersect, index 0)    
            new TestSpec(5, 19.0)
            {
                startindex = 2,
                initialValue = 47.2,
                initialIntersection = 9.2,
                targetValue = -4.5,
                direction = GeneratorDirection.Backward,

                ASSERT_index = 0,
                ASSERT_intersection = 0.0,
                ASSERT_valuereached = 0.0
            }.Run();

            // ↓ initial (0.0 offset, 0.0 intersect)
            // |---|---|---|---|---| 
            //↑ target (-4.5 offset, resets to 0.0 intersect, index 0)    
            new TestSpec(5, 19.0)
            {
                startindex = 0,
                initialValue = 0.0,
                initialIntersection = 0.0,
                targetValue = -4.5,
                direction = GeneratorDirection.Backward,

                ASSERT_index = 0,
                ASSERT_intersection = 0.0,
                ASSERT_valuereached = 0.0
            }.Run();

            // ↓ initial (0.2 offset, 0.0 intersect)
            // |---|---|---|---|---| 
            //↑ target (-4.5 offset, resets to 0.0 intersect, index 0)    
            new TestSpec(5, 19.0)
            {
                startindex = 0,
                initialValue = 0.2,
                initialIntersection = 0.2,
                targetValue = -4.5,
                direction = GeneratorDirection.Backward,

                ASSERT_index = 0,
                ASSERT_intersection = 0.0,
                ASSERT_valuereached = 0.0
            }.Run();

            //   ↓ initial (8.7 offset, 8.7 intersect)
            // |---|---|---|---|---| 
            //                       ↑ target (100.0 offset, resets to 19.0 intersect, index 4)    
            new TestSpec(5, 19.0)
            {
                startindex = 0,
                initialValue = 8.7,
                initialIntersection = 8.7,
                targetValue = 100.0,
                direction = GeneratorDirection.Forward,

                ASSERT_index = 4,
                ASSERT_intersection = 19.0,
                ASSERT_valuereached = 95.0
            }.Run();
            //                  ↓ initial (83.7 offset, 7.7 intersect)
            // |---|---|---|---|---| 
            //                       ↑ target (100.0 offset, resets to 19.0 intersect, index 4)    
            new TestSpec(5, 19.0)
            {
                startindex = 4,
                initialValue = 83.7,
                initialIntersection = 7.7,
                targetValue = 100.0,
                direction = GeneratorDirection.Forward,

                ASSERT_index = 4,
                ASSERT_intersection = 19.0,
                ASSERT_valuereached = 95.0
            }.Run();

            // - is 3.0
            //            ↓ initial (25.0 offset, 1.0 intersect)
            // |---|-----|-|--|--------------|------|--------------------|
            // | 9 |  15 |3|6 |    42        |  18  |    60              |
            // |---|-----|-|--|--------------|------|--------------------|
            //                       ↑ target (52.0 offset, 19.0 intersect)    
            new TestSpec(9, 15, 3, 6, 42, 18, 60)
            {
                startindex = 2,
                initialValue = 25.0,
                initialIntersection = 1.0,
                targetValue = 52.0,
                direction = GeneratorDirection.Forward,

                ASSERT_index = 4,
                ASSERT_intersection = 19.0,
                ASSERT_valuereached = 52.0
            }.Run();
        }

        class TestSpec
        {
            readonly IList<double> items;
            public TestSpec(int n, double uniform)
            {
                items = new List<double>(from i in Enumerable.Range(0, n) select uniform);
            }
            public TestSpec(params double[] heights)
            {
                items = new List<double>(heights);
            }

            public int startindex, ASSERT_index;
            public double initialIntersection, initialValue, targetValue, ASSERT_intersection, ASSERT_valuereached;
            public GeneratorDirection direction;
            public void Run()
            {
                var mock = new IntersectionToolsMock(items);
                var test = new IntersectionFinder.FindIntersecionAndOffsetArgs
                {
                    initialIntersection = initialIntersection,
                    initialValue = initialValue,
                    targetValue = targetValue
                };
                mock.tools.direction = direction;
                int index = startindex;
                var result = IntersectionFinder.FindIntersection(test, mock.tools, ref index);
                Debug.Assert(index == ASSERT_index);

                Debug.Assert(DoubleUtil.AreClose(result.intersection, ASSERT_intersection));
                Debug.Assert(DoubleUtil.AreClose(result.valueReached, ASSERT_valuereached));
            }
        }
        class IntersectionToolsMock
        {
            public readonly IntersectionFinder.FindIntersectionTools tools;
            readonly IList<double> itemHeights;
            public IntersectionToolsMock(IList<double> itemHeights)
            {
                this.itemHeights = itemHeights;
                tools = new IntersectionFinder.FindIntersectionTools
                {
                    total_items = itemHeights.Count,
                    generator = new Gen(itemHeights.Count),
                    get_desired_height = MeasureHeight,
                    temporarily_realize_item = TempReal
                };
            }
            Action TempReal(DependencyObject iitm)
            {
                var itm = iitm as Gen.itm;
                if (itm.realized)
                    throw new InvalidOperationException("Already realized! Dont twice!");
                itm.realized = true;
                itm.height = itemHeights[itm.index];
                return () => itm.realized = false;
            }
            double MeasureHeight(DependencyObject iitm)
            {
                var itm = iitm as Gen.itm;
                if (!itm.realized)
                    throw new InvalidOperationException("This wasn't realized! It would measure sth silly.");
                return itm.height;
            }
            class Gen : IItemContainerGenerator
            {
                readonly int nmax;
                public Gen(int nmax)
                {
                    this.nmax = nmax;
                }
                void AssertValidIndex(int i)
                {
                    if (i < 0 || i >= nmax)
                        throw new InvalidOperationException("Index went out of bounds! Stop trying to Generate or whatever!");
                }

                public class itm : DependencyObject
                {
                    public int index;
                    public double height;
                    public bool realized = false;
                    public bool prepared = false;
                }

                Dictionary<int, itm> generated = new Dictionary<int, itm>();
                int? current = null;
                GeneratorDirection? current_direction;
                public DependencyObject GenerateNext(out bool isNewlyRealized)
                {
                    if (!current.HasValue)
                        throw new InvalidOperationException("Not started. call startat. the use this method. dispose when done. repeat if neccesary.");
                    AssertValidIndex(current.Value);
                    var itm = generated.ContainsKey(current.Value) ? generated[current.Value] : generated[current.Value] = new itm { index = current.Value };
                    isNewlyRealized = itm.prepared;
                    current += current_direction.Value == GeneratorDirection.Forward ? 1 : -1;
                    return itm;
                }
                public void PrepareItemContainer(DependencyObject container)
                {
                    if (!(container is itm))
                        throw new ArgumentException("Can't prepare this type! I didnt Generate it!");
                    var itm = container as itm;
                    if (itm.prepared)
                        throw new InvalidOperationException("This one was already preapred!");
                    itm.prepared = true;
                }
                public GeneratorPosition GeneratorPositionFromIndex(int itemIndex)
                {
                    AssertValidIndex(itemIndex);
                    return new GeneratorPosition { Index = itemIndex };
                }
                public IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction, bool allowStartAtRealizedItem)
                {
                    if (generated.ContainsKey(position.Index) && !allowStartAtRealizedItem)
                        throw new ArgumentException("Can't start at a generated item if you set that arg!");
                    AssertValidIndex(position.Index);
                    current = position.Index;
                    current_direction = direction;
                    return new dact(() =>
                    {
                        current = null;
                        current_direction = null;
                    });
                }

                class dact : IDisposable
                {
                    readonly Action act;
                    public dact(Action act) { this.act = act; }
                    public void Dispose() { act(); }
                }

                // we dont call these!
                public DependencyObject GenerateNext() { throw new NotImplementedException(); }
                public ItemContainerGenerator GetItemContainerGeneratorForPanel(Panel panel) { throw new NotImplementedException(); }
                public int IndexFromGeneratorPosition(GeneratorPosition position) { throw new NotImplementedException(); }
                public void Remove(GeneratorPosition position, int count) { throw new NotImplementedException(); }
                public void RemoveAll() { throw new NotImplementedException(); }
                public IDisposable StartAt(GeneratorPosition position, GeneratorDirection direction) { throw new NotImplementedException(); }
            }
        }
    }

}
