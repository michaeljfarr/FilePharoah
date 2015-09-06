using System.Threading;

namespace EtoTest.IO
{
    class WeakSemaphore
    {
        private int _status;
        
        public void Release()
        {
            _status = 0;
        }

        public bool Access()
        {
            return Interlocked.Increment(ref _status) == 1;
        }
    }
}