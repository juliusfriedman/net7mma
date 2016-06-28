using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nVideo.Common;

namespace nVideo.Codecs.H264
{
    public class NodeBox : Box
    {
        private static int MAX_BOX_SIZE = 128 * 1024 * 1024;
        protected List<Box> boxes = new List<Box>();
        protected BoxFactory factory = BoxFactory.getDefault();

        public NodeBox(Header atom)
            : base(atom)
        {

        }

        public NodeBox(NodeBox other)
            : base(other)
        {
            this.boxes = other.boxes;
            this.factory = other.factory;
        }

        public override void parse(MemoryStream input)
        {

            while (input.remaining() >= 8)
            {
                Box child = parseChildBox(input, factory);
                if (child != null)
                    boxes.Add(child);
            }
        }

        public static Box parseChildBox(MemoryStream input, BoxFactory factory)
        {
            MemoryStream fork = input.duplicate();
            while (input.remaining() >= 4 && fork.getInt() == 0)
                input.getInt();
            if (input.remaining() < 4)
                return null;

            Header childAtom = Header.read(input);
            if (childAtom != null && input.remaining() >= childAtom.getBodySize())
                return parseBox(StreamExtensions.read(input, (int)childAtom.getBodySize()), childAtom, factory);
            else
                return null;
        }

        public static Box newBox(Header header, BoxFactory factory)
        {
            Box claz = factory.ToClass(header.getFourcc());
            if (claz == null)
                return new LeafBox(header);
            try
            {
                try
                {
                    return null;
                    //return claz.getConstructor(Header.gclass).newInstance(header);
                }
                catch (Exception e)
                {
                    return claz.newInstance();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static Box parseBox(MemoryStream input, Header childAtom, BoxFactory factory)
        {
            Box box = newBox(childAtom, factory);

            if (childAtom.getBodySize() < MAX_BOX_SIZE)
            {
                box.parse(input);
                return box;
            }
            else
            {
                return new LeafBox(new Header("free", 8));
            }
        }

        public List<Box> getBoxes()
        {
            return boxes;
        }

        public void Add(Box box)
        {
            boxes.Add(box);
        }

        protected override void doWrite(MemoryStream outb)
        {
            foreach (var box in boxes)
            {
                box.write(outb);
            }
        }

        public void addFirst(MovieHeaderBox box)
        {
            boxes.Insert(0, box);
        }

        public void replace(String fourcc, Box box)
        {
            removeChildren(fourcc);
            Add(box);
        }

        public void replace(Box box)
        {
            removeChildren(box.getFourcc());
            Add(box);
        }

        //protected void dump(StringBuilder sb) {
        //    sb.append("{\"tag\":\"" + header.getFourcc() + "\",");
        //    List<String> fields = new ArrayList<String>(0);
        //    collectModel(this.getClass(), fields);
        //    ToJSON.fieldsToJSON(this, sb, fields.toArray(new String[0]));
        //    sb.append("\"boxes\": [");
        //    dumpBoxes(sb);
        //    sb.append("]");
        //    sb.append("}");
        //}

        protected void getModelFields(List<String> model)
        {

        }

        protected void dumpBoxes(StringBuilder sb)
        {
            for (int i = 0; i < boxes.Count(); i++)
            {
                boxes[i].dump(sb);
                if (i < boxes.Count() - 1)
                    sb.Append(",");
            }
        }

        public void removeChildren(params String[] fourcc)
        {
            foreach (var it in boxes)
            {
                String fcc = it.getFourcc();
                foreach (var cand in fourcc)
                {
                    if (cand.Equals(fcc))
                    {
                        break;
                    }
                }
            }
        }
    }
}
