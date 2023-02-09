using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace DetectLinesInPicture {
    public class ComponentDraw : GH_Component {
        public ComponentDraw() : base("Draw", "Draw",
            "Nakreslí v Rhinu meshovì obrázek po pixelech, Mode znaèí jak veliké mají být pixely. Pokuï jde o výkon použijte jiný zpùsob. Meshe mají VertexColors!\nDraw in Rhido image. Mesh has VertexColors!",
            "Detector", "Detecting") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap",      "Bitmap",       "Obrázek\nBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter ("Pixel size",  "Mode",         "Malé èísla jsou pomalé, nemusí se vykreslit", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter ("Number",      "Transparency", "0=zcela prùhledné, 1=neprùhlené\n0=full transparent, 1=opaque", GH_ParamAccess.item, 1);
            pManager.AddPointParameter  ("Point",       "Position",     "Pozice kde umístit\nPosition", GH_ParamAccess.item, new Point3d(0,0,0));
            pManager.AddVectorParameter ("Vector",      "Scale",        "Roztáhnout, smrsknout, ozrcadlit (z se neuvažuje)\nScale, flip, (z - don't care)", GH_ParamAccess.item, new Vector3d(1,1,0));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh of image", GH_ParamAccess.item);   
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            // naèíst
            Bitmap bitmap = null;
            if (!DA.GetData(0, ref bitmap)) {
                DA.AbortComponentSolution();
                return;
            } 
            
            double RawQuality = 0.5;
            if (DA.GetData(1, ref RawQuality)) {
                if (RawQuality<=1) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Quality should be bigger than zero");
                    DA.AbortComponentSolution();
                    return;
                }
            }
            int quality=(int)RawQuality;

            double RawAlpha = 1.0;
            DA.GetData(2, ref RawAlpha);
            if (RawAlpha<0)RawAlpha=0;
            else if (RawAlpha>1)RawAlpha=1;
            int alpha=(int)(RawAlpha*255);
            if (alpha==0){ 
                DA.SetData(0, new Mesh());  
                return;
            }
           
            Point3d Point = new Point3d(0,0,0);
            DA.GetData(3, ref Point);

            Vector3d Vector = new Vector3d(1,1,0);
            DA.GetData(4, ref Vector);

            int PictureWidth=bitmap.Width;
            int PictureHeight=bitmap.Height;  
            Rectangle rec = new Rectangle(0, 0, PictureWidth, PictureHeight);
                       
            BitmapData data = bitmap.LockBits(rec, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            byte* pointer = (byte*)data.Scan0;
           
            // Pøiprav tile s barvou
            // Øádková optimalizace
            List<Tile> listTiles=new List<Tile>();
            {
                int Stride=data.Stride;
                double endWidth=((PictureWidth/quality)*quality)*Vector.X+Point.X;

                for (int y = 0; y < PictureHeight; y += quality) {
                    byte* row = pointer + y*Stride;
                    Color lastColor=Color.Transparent;
                    int start=-1;

                    // Vykresli... jako slova v bloku
                    for (int x = 0; x < PictureWidth; x += quality) {
                        byte* headR = row + x*3;
                        Color color=Color.FromArgb(alpha, *(headR + 2), *(headR + 1), *headR);   // ??? red <-> blue

                        if (start==-1) {
                            start=x;
                            lastColor=color;
                        } else if (color!=lastColor) { 
                            listTiles.Add(new Tile{ X=start*Vector.X+Point.X, X2=x*Vector.X+Point.X, Y=y*Vector.Y+Point.Y, Color=lastColor});
                            lastColor=color;
                            start=x;                            
                        }
                    }

                    // Na konci øádku
                    if (start!=-1) { 
                        listTiles.Add(new Tile{ X=start*Vector.X+Point.X, X2=endWidth, Y=y*Vector.Y+Point.Y, Color=lastColor});
                    }
                }
            }

            // Vytvoø mesh
            Mesh mesh=new Mesh();

            // Pøidat tile
            foreach (Tile title in listTiles) title.AddFace(mesh,quality);
            mesh.Normals.ComputeNormals();
       
            // Pøiøadit tile barvu
            {
                mesh.VertexColors.CreateMonotoneMesh(Color.White);
                int ii=-4;
                foreach (Tile tile in listTiles) tile.AddColor(mesh, ii+=4);
            }

            mesh.Compact();
            bitmap.UnlockBits(data);

            DA.SetData(0, mesh);
        }
                
        protected override Bitmap Icon => Properties.Resources.picture;

        public override Guid ComponentGuid => new Guid("44712965-93E3-43EE-BDC4-3257A082F521");

        class Tile { 
            public double X, X2, Y;
            public Color Color;

            // Pøidat obdélnikové  
            public void AddFace(Mesh mesh, int size) { 
                mesh.Vertices.Add(new Point3d(X,    Y,      0));
                mesh.Vertices.Add(new Point3d(X2,   Y,      0));
                mesh.Vertices.Add(new Point3d(X2,   Y+size, 0));
                mesh.Vertices.Add(new Point3d(X,    Y+size, 0));

                int cnt=mesh.Vertices.Count;
                mesh.Faces.AddFace(cnt+0, cnt+1, cnt+2, cnt+3);
            }
            
            // Nastavení barvy
            public void AddColor(Mesh mesh, int offset) { 
                mesh.VertexColors[offset  ] = Color;
                mesh.VertexColors[offset+1] = Color;
                mesh.VertexColors[offset+2] = Color;
                mesh.VertexColors[offset+3] = Color;
            }
        }
    } 
}