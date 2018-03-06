using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using VirtualizingUtils;
using static VirtualizingUtils.IntersectionFinder;

namespace VVStackPanel.WPF
{
    class VStackAdapter
    {
        readonly FindIntersectionTools<DependencyObject> tools;
        readonly IItemContainerGenerator generator;
        public VStackAdapter(IItemContainerGenerator generator, FindIntersectionTools<DependencyObject> baseTools)
        {
            tools = baseTools;
            tools.generate_next = () =>
            {
                var cld = generator.GenerateNext(out bool nr);
                if (nr) generator.PrepareItemContainer(cld);
                return cld;
            };
            tools.get_desired_height = cldo =>
            {
                var cld = cldo as UIElement;
                cld.Measure(new Size(availableWidth, double.PositiveInfinity));
                return cld.DesiredSize.Height;
            };
        }
        public double availableWidth;
        public FindIntersectionResult FindIntersection(FindIntersecionAndOffsetArgs args, ref int firstIndex)
        {
            GeneratorPosition startPos = generator.GeneratorPositionFromIndex(firstIndex);
            Func<GeneratorDirection> gd = ()=> tools.forwardDirection ? GeneratorDirection.Forward : GeneratorDirection.Backward;
            tools.reset_generator = () => generator.StartAt(startPos, gd(), true);
            return FindIntersection<DependencyObject>(args, tools, ref firstIndex);
        }
    }
}
