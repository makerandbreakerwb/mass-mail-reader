namespace MassMailReader;

public static class ParalelizationSplit
{
    public static List<List<T>> SplitForParalelization<T>(IEnumerable<T> values, int splits = 0)
    {
        splits = splits < 1 ? Environment.ProcessorCount : splits;
        var result = new List<List<T>>(splits);
        for (var i = 0; i < splits; i++)
        {
            result.Add([]);
        }

        var index = 0;
        foreach (var value in values)
        {
            result[index].Add(value);
            index = (index + 1) % splits;
        }

        return result;
    }
}
