using System;
using System.Threading.Tasks;

namespace GameEngine
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var engine = new GameEngine();
                engine.RunEngine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("엔진 종료");
            }
        }
    }
}
