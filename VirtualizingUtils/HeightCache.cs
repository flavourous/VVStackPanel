using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualizingUtils
{
    public class HeightCache
    {
        const int N_CACHE = 50;
        double lastWidth = 0.0;
        Dictionary<int, LinkedListNode<double>> llpos = new Dictionary<int, LinkedListNode<double>>();
        Dictionary<LinkedListNode<double>, int> reverse_lookup = new Dictionary<LinkedListNode<double>, int>();
        LinkedList<double> heightOrder = new LinkedList<double>();
        public void Push(int index, double height, double width)
        {
            if (width != lastWidth)
            {
                lastWidth = width;
                llpos.Clear();
                heightOrder.Clear();
            }

            if (llpos.Count > N_CACHE)
            {
                int i = 0;
                while (i++ < N_CACHE / 2)
                {
                    var curr = heightOrder.First;
                    var idx = reverse_lookup[curr];
                    llpos.Remove(idx);
                    reverse_lookup.Remove(curr);
                    heightOrder.RemoveFirst();
                }
            }

            if (llpos.ContainsKey(index))
            {
                var lln = llpos[index];
                reverse_lookup.Remove(lln);
                heightOrder.Remove(lln);
            }
            reverse_lookup[llpos[index] = heightOrder.AddLast(height)] = index;
        }
        public double GetAverageHeight()
        {
            if (heightOrder.Count == 0) return 0.0;
            return heightOrder.Average();
        }
    }


}
