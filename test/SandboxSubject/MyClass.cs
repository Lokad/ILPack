using System;
using System.Threading.Tasks;

namespace SandboxSubject
{
    public class MyClass
    {

        public async Task<int> AsyncMethod(int x, int y)
        {
            await Task.Delay(100);
            return x + y;
        }
    }
}
