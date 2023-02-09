using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace DetectLinesInPicture {
    public class ComponentPreviewImage : GH_Component {
       public Bitmap bitmapPrev=null;
        public ComponentPreviewImage()  : base("Bitmap Preview", "Bitmap Preview",
            "Zobrazit náhlad obrázku\nBitmap Prewiew",
            "Detector", "Basic") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        public override void CreateAttributes() {
            m_attributes = new CustomParameterAttributes(this) {
                Bounds = new RectangleF(0, 0, 10, 10)
            };
        }

        public class CustomParameterAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes {
            public Bitmap bitmap;
            ComponentPreviewImage parent;
            public CustomParameterAttributes (ComponentPreviewImage owner) : base(owner) {
                parent=owner; 
                Bounds =new RectangleF(0,0,100,100);
            }
     
            // Zobrazit obrázek
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel) {
                base.Render(canvas, graphics, channel); 

                // Obrázek není přiřazen
                if (parent.bitmapPrev==null) return;

                // w, h = vypočítat dle poměru stran
                int w,h;
                if (parent.bitmapPrev.Width<parent.bitmapPrev.Height){
                    w=80; 
                    h=(int)(80*parent.bitmapPrev.Height/(float)(parent.bitmapPrev.Width));
                } else {
                    w=(int)(80*((float)parent.bitmapPrev.Width/parent.bitmapPrev.Height));
                    h=80;
                }

                // Pozice komponenty
                PointF p = Pivot;

                // vykreslit obrázek
                graphics.DrawImage(parent.bitmapPrev,new Rectangle((int)p.X-10, (int)p.Y-h/2, w, h));
            }
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            Bitmap bitmap=null;

            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }

            bitmapPrev =bitmap;
        }

        protected override Bitmap Icon=>null;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-799AA04BFD52");
    }
}