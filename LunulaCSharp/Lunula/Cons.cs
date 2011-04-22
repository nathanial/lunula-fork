using System.Collections.Generic;
using System.Linq;

namespace Lunula
{
    public class Cons {
        object _car;
        object _cdr;
        public Cons(object car, object cdr) {
            _car = car;
            _cdr = cdr;
        }
        public static object Car(object cons) {
            if (cons is Cons) return ((Cons)cons)._car;
            if (cons == null) return cons;
            throw new LunulaException(string.Format("object {0} is not a cons", cons));
        }
        public static object Cdr(object cons) {
            if (cons is Cons) return ((Cons)cons)._cdr;
            if (cons == null) return cons;
            throw new LunulaException(string.Format("object {0} is not a cons", cons));
        }
        public static object SetCar(object c, object v) {
            if (c is Cons) ((Cons)c)._car = v;
            else throw new LunulaException(string.Format("object {0} is not a cons", c));
            return Void.TheVoidValue;
        }
        public static object SetCdr(object c, object v) {
            if (c is Cons) ((Cons)c)._cdr = v;
            else throw new LunulaException(string.Format("object {0} is not a cons", c));
            return Void.TheVoidValue;
        }
        public static object Reverse(object cons) {
            object reversed = null;
            while (cons != null) {
                reversed = new Cons(Car(cons), reversed);
                cons = Cdr(cons);
            }
            return reversed;
        }
        public static int Length(object cons) {
            int length = 0;
            while (cons != null) {
                length++;
                cons = Cdr(cons);
            }
            return length;
        }

        public static object[] ToReverseObjectArray(object cons) {
            var length = Length(cons);
            var objs = new object[length];
            for (var x = 0; x < length; x++) {
                objs[length - 1 - x] = Car(cons);
                cons = Cdr(cons);
            }
            return objs;
        }
        public static object[] ToObjectArray(object cons) {
            var length = Length(cons);
            var objs = new object[length];
            for (var x = 0; x < length; x++) {
                objs[x] = Car(cons);
                cons = Cdr(cons);
            }
            return objs;
        }
        public static object ConsFromIEnumerable(IEnumerable<object> collection) {
            var c = collection.Aggregate<object, object>(null, (current, thing) => new Cons(thing, current));
            return Reverse(c);
        }
        public static object ConsFromArray(object[] array) {
            var c = array.Aggregate<object, object>(null, (current, t) => new Cons(t, current));
            return Reverse(c);
        }
    }
}