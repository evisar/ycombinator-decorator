using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Serialization;

namespace YCombinatorStyleActionDecorator
{
    [Decorate(Decorator.All)]
    public class TransferTo 
    {
        public Sale Sale { get; set; }
        public Location Location { get; set; }
    }

    [Flags]
    public enum Decorator
    {
        None = 0,
        Logging,
        Transactions,
        Performance,
        All = ~0
    }

    public class DecorateAttribute : Attribute
    {
        public Decorator Type { get; private set; }

        public DecorateAttribute(Decorator type)
        {
            this.Type = type;
        }
    }


    public class Decorator<T>
    {
        readonly static Decorator _decorator;

        static Decorator()
        {
            var d = typeof(T).GetCustomAttributes(typeof(DecorateAttribute), false).OfType<DecorateAttribute>().FirstOrDefault();
            if(d!=null) _decorator = d.Type;
        }

        static ILogger _logger;

        public Decorator(ILogger logger)
        {
            _logger = logger;
        }

        IEnumerable<Action<Action<T>,T>> GetDecorators()
        {
            if(_decorator.HasFlag(Decorator.Logging))
            {
                yield return (x, y) =>
                {
                    try
                    {
                        _logger.Info(string.Format("Entering: {0}", typeof(TransferTo).FullName));
                        x(y);
                        _logger.Info(string.Format("Finishing: {0}", typeof(TransferTo).FullName));
                    }
                    catch(Exception ex)
                    {
                        _logger.Error(ex.Message, ex);
                        throw;
                    }
                };;
            }
            if(_decorator.HasFlag(Decorator.Transactions))
            {
                yield return (x, y) =>
                {
                    using (var tran = new TransactionScope())
                    {
                        _logger.Info(string.Format("Entering Transaction: {0}", typeof(TransferTo).FullName));
                        x(y);
                        _logger.Info(string.Format("Finishing Transaction: {0}", typeof(TransferTo).FullName));
                    }
                };
            }
            if(_decorator.HasFlag(Decorator.Performance))
            {
                yield return (x, y) =>
                {
                    var sw = Stopwatch.StartNew();
                    x(y);
                    sw.Stop();
                    _logger.Info(string.Format("Elapsed: {0}", sw.Elapsed));
                };
            }
        }

        /// <summary>
        /// Y-Combinator for decorative workflows
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        static Action<T> Y(Action<T> y, params Action<Action<T>, T>[] z)
        {
            if (z.Length == 0)
                return y;
            return Y(x => z.Last()(y, x), z.Take(z.Length - 1).ToArray());
        }

        public Action<T> Decorate(Action<T> action)
        {
            return Y(action, GetDecorators().ToArray());
        }
    }

    public class Sale
    {
    }

    public class Location
    {
    }

    public class SaleTransfer
    {
        public void TransferTo(Sale sale, Location location)
        {

        }
    }

    public class SaleTransferHandler
    {
        readonly ILogger _logger;

        public SaleTransferHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(TransferTo transferTo)
        {
            _logger.Info(string.Format("Processing... {0}", typeof(TransferTo).FullName));
            var ser = new XmlSerializer(typeof(TransferTo));
            var sb = new StringBuilder();
            ser.Serialize(new StringWriter(sb), transferTo);
            _logger.Info(sb.ToString());

            var transfer = new SaleTransfer();
            transfer.TransferTo(transferTo.Sale, transferTo.Location);

            _logger.Info("Done.");            
        }
    }

    class Program
    {
        static ILogger _logger = new ConsoleLogger();
        static void Main()
        {

            var t = new TransferTo();
            t.Sale = new Sale();
            t.Location = new Location();

            var transfer = new SaleTransferHandler(_logger);

            var workflow = new Decorator<TransferTo>(_logger).Decorate(transfer.Handle);
            workflow(t);

            Console.ReadLine();
        }



    }
}
