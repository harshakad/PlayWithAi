using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GamePlatform.Infrastructure.Agents
{
    /// <summary>
    /// Provides an AI agent tool to play the next move using Stockfish, given a FEN string.
    /// </summary>
    public class StockfishTool : IDisposable
    {
        private readonly string _stockfishPath;
        private readonly int _depth;
        private readonly Process _sfProcess;
        private readonly StreamWriter _sw;
        private readonly StreamReader _sr;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public StockfishTool(string stockfishPath = "C:\\stockfish\\stockfish-windows-x86-64-avx2.exe", int depth = 10)
        {
            _stockfishPath = stockfishPath;
            _depth = depth;

            _sfProcess = new Process();
            _sfProcess.StartInfo.FileName = _stockfishPath;
            _sfProcess.StartInfo.UseShellExecute = false;
            _sfProcess.StartInfo.RedirectStandardInput = true;
            _sfProcess.StartInfo.RedirectStandardOutput = true;
            _sfProcess.StartInfo.CreateNoWindow = true;
            _sfProcess.Start();

            _sw = _sfProcess.StandardInput;
            _sr = _sfProcess.StandardOutput;

            // Initialize UCI
            _sw.WriteLine("uci");
            _sw.Flush();

            // Wait for Stockfish to be ready
            string? line;
            while ((line = _sr.ReadLine()) != null)
            {
                if (line.StartsWith("uciok"))
                    break;
            }
        }

        /// <summary>
        /// Gets the best move from Stockfish for the given FEN string asynchronously.
        /// Returns the move as a UCI string (e.g., "e2e4").
        /// </summary>
        public async Task<string> GetBestMoveAsync(string fen)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(StockfishTool));

                // Set position
                await _sw.WriteLineAsync($"position fen {fen}");
                await _sw.FlushAsync();

                // Start analysis
                await _sw.WriteLineAsync($"go depth {_depth}");
                await _sw.FlushAsync();

                // Read output for bestmove
                string? line;
                string? bestMove = null;
                while ((line = await _sr.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("bestmove"))
                    {
                        var parts = line.Split(' ');
                        if (parts.Length >= 2)
                            bestMove = parts[1];
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(bestMove))
                    throw new InvalidOperationException("Stockfish did not return a best move.");

                return bestMove;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore.Wait();
            try
            {
                if (_disposed) return;
                try
                {
                    _sw.WriteLine("quit");
                    _sw.Flush();
                }
                catch { }
                try { _sfProcess.WaitForExit(2000); } catch { }
                try { _sw.Dispose(); } catch { }
                try { _sr.Dispose(); } catch { }
                try { _sfProcess.Dispose(); } catch { }
                _disposed = true;
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
        }
    }
}