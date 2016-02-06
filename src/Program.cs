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

    [Decorate(typeof(Performance<>))]
    [Decorate(typeof(Logging<>))]
    [Decorate(typeof(Transaction<>))]
    public class TransferTo 
    {
        public Sale Sale { get; set; }
        public Location Location { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class DecorateAttribute : Attribute
    {
        public Type Type { get; private set; }

        public DecorateAttribute(Type type)
        {
            this.Type = type;
        }
    }

    public class Logging<T>  : IDecorator<T>
    {
        readonly ILogger _logger;
        public Logging(ILogger logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
        }
        public  Action<Action<T>, T> GetAction()
        {
            return (x, y) =>
            {
                try
                {
                    _logger.Info(string.Format("Entering: {0}", typeof(TransferTo).FullName));
                    x(y);
                    _logger.Info(string.Format("Finishing: {0}", typeof(TransferTo).FullName));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                    throw;
                }
            };
        }
    }

    public class Transaction<T>: IDecorator<T>
    {
        readonly ILogger _logger;
        public Transaction(ILogger logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
        }
        public Action<Action<T>, T> GetAction()
        {
            return (x, y) =>
            {
                using (var tran = new TransactionScope())
                {
                    _logger.Info(string.Format("Entering Transaction: {0}", typeof(TransferTo).FullName));
                    x(y);
                    _logger.Info(string.Format("Finishing Transaction: {0}", typeof(TransferTo).FullName));
                }
            };
        }
    }

    public class Performance<T> : IDecorator<T>
    {
        readonly ILogger _logger;
        public Performance(ILogger logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
        }
        public Action<Action<T>, T> GetAction()
        {
            return (x, y) =>
            {
                using (var tran = new TransactionScope())
                {
                    var sw = Stopwatch.StartNew();
                    x(y);
                    sw.Stop();
                    _logger.Info(string.Format("Elapsed: {0}", sw.Elapsed));
                }
            };
        }
    }

    public interface IDecorator<T>
    {
        Action<Action<T>,T> GetAction();
    }


    public class Decorators<T>
    {

        readonly ILogger _logger;
        public Decorators(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Y-Combinator for decorative workflows
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static Action<T> Y(Action<T> x, IEnumerable<Action<Action<T>, T>> y)
        {
            if (y.Count() == 0)
                return x;
            return 
                Y(a => y.First()(x, a), y.Skip(1).Take(y.Count() - 1));
        }

        static IEnumerable<Action<Action<T>, T>> _decorators;
        public Action<T> Decorate(Action<T> action)
        {
            if (_decorators == null)
            {
                _decorators = (from t in typeof(T).GetCustomAttributes(typeof(DecorateAttribute), false).OfType<DecorateAttribute>()
                              let d = Activator.CreateInstance(t.Type.MakeGenericType(typeof(T)), _logger) as IDecorator<T>
                              select d.GetAction()).Reverse();
                //we reverse the list to make sure that the decorator on top is executed first
            }

            return Y(action, _decorators);
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

            var workflow = new Decorators<TransferTo>(_logger).Decorate(transfer.Handle);
            workflow(t);

            Console.ReadLine();
        }



    }
}
