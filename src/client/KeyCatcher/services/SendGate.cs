namespace KeyCatcher.services
{
    public class SendGate
    {
        private bool _blocked;
        public bool IsBlocked => _blocked;

        // Fired whenever block state changes (for UI if needed)
        public event Action<bool>? BlockChanged;

        /// <summary>
        /// Try to run a send. Blocks other sends until it completes.
        /// </summary>
        public async Task<bool> TrySendAsync(Func<Task<bool>> sendFunc)
        {
            if (_blocked)
                return false;

            try
            {
                _blocked = true;
                BlockChanged?.Invoke(true);

                return await sendFunc();
            }
            finally
            {
                _blocked = false;
                BlockChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Manually stop block (rarely needed now).
        /// </summary>
        public void StopBlock()
        {
            _blocked = false;
            BlockChanged?.Invoke(false);
        }
    }
}