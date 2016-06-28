using nVideo.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nVideo.Codecs.H264
{
    public abstract class Box
    {
        private static String GET_MODEL_FIELDS = "getModelFields";
        protected Header header;

        public Box(Header header)
        {
            this.header = header;
        }

        public Box(Box other)
        {
            this.header = other.header;
        }

        public Header getHeader()
        {
            return header;
        }

        public abstract void parse(MemoryStream buf);

        public static Box findFirst(NodeBox box, params String[] path)
        {
            //return findFirst(box, Box.gclass, path);

            return null;
        }

        internal string getFourcc()
        {
            throw new NotImplementedException();
        }

        public static T findFirst<T>(NodeBox box, T clazz, params String[] path)
        {
            //T[] result = (T[])findAll(box, clazz, path);

            //return result.Length > 0 ? result[0] : null;

            return default(T);
        }

        public static Box[] findAll(Box box, params String[] path)
        {
           // return findAll(box, Box.gclass, path);
            return null;
        }

        private static void findSub(Box box, List<String> path, ICollection<Box> result)
        {

            if (path.Count() > 0)
            {
                String head = path[0];
                path.RemoveAt(0);
                if (box is NodeBox)
                {
                    NodeBox nb = (NodeBox)box;
                    foreach (var candidate in nb.getBoxes())
                    {
                        if (head == null || head.Equals(candidate.header.getFourcc()))
                        {
                            findSub(candidate, path, result);
                        }
                    }
                }
                path.Insert(0, head);
            }
            else
            {
                result.Add(box);
            }
        }

        public static T findAll<T>(Box box, T class1, params String[] path)
        {
            List<Box> result = new List<Box>();
            List<String> tlist = new List<String>();
            foreach (var type in path)
            {
                tlist.Add(type);
            }
            findSub(box, tlist, result);
            //return result.ToArray((T[])Array.Create(class1, 0));
            return default(T);
        }

        public void write(MemoryStream buf)
        {
            MemoryStream dup = buf.duplicate();
            StreamExtensions.skip(buf, 8);
            doWrite(buf);

            header.setBodySize(buf.position() - dup.position() - 8);
            //Assert.assertEquals(header.headerSize(), 8);
            header.write(dup);
        }

        protected abstract void doWrite(MemoryStream outb);

        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            //dump(sb);
            return sb.ToString();

        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool equals(Object obj)
        {
            if (obj == null)
                return false;
            return ToString().Equals(obj.ToString());
        }

        protected void collectModel<T>(T claz, List<String> model)
        {
            //if (Box.gclass == claz || !Box.gclass.isAssignableFrom(claz))
            //    return;

            //collectModel(claz.getSuperclass(), model);

            //try {
            //    Method method = claz.getDeclaredMethod(GET_MODEL_FIELDS, List.gclass);
            //    method.invoke(this, model);
            //} catch (NoSuchMethodException e) {
            //    checkWrongSignature(claz);
            //    model.addAll(ToJSON.allFields(claz));
            //} catch (Exception e) {
            //}
        }

        private void checkWrongSignature(object claz)
        {
            //for (Method method : claz.getDeclaredMethods()) {
            //    if (method.getName().equals(GET_MODEL_FIELDS)) {
            //        Logger.warn("Class " + claz.getCanonicalName() + " contains 'getModelFields' of wrong signature.\n"
            //                + "Did you mean to define 'protected void " + GET_MODEL_FIELDS + "(List<String> model) ?");
            //        break;
            //    }
            //}
        }

        //public static <T extends Box> T as(Class<T> class1, LeafBox box) {
        //    try {
        //        T res = class1.getConstructor(Header.class).newInstance(box.getHeader());
        //        res.parse(box.getData());
        //        return res;
        //    } catch (Exception e) {
        //        throw e;
        //    }
        //}

        internal static Box findFirst(VideoSampleEntry vse, string p)
        {
            throw new NotImplementedException();
        }

        internal void dump(StringBuilder sb)
        {
            throw new NotImplementedException();
        }

        internal Box newInstance()
        {
            throw new NotImplementedException();
        }

        internal object getConstructor(object p)
        {
            throw new NotImplementedException();
        }
    }
}
