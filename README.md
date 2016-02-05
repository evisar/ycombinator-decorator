# ycombinator-decorator

Take an action ofon top f an argument Action<T>.
Say you want to apply a number of other actions before this action, without applying any type of AOP or reflection.

I decided to use Y-Combinator to create a function wrapper which chain each other until finally the original action is called.

Here's the Y-Combinator code:

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


We can pass an array of decorators of Action<Action<T>,T> and they will wrapp the original action, and the whole call chain will 
become to look as a sort of onion layers. :) 

![alt tag](onion.png)
