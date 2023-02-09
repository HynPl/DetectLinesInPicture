using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace DetectLinesInPicture {
    public class ComponentDetectLines : GH_Component {
        public ComponentDetectLines() : base("Detect Lines", "Lines detection",
            "Description",
            "Detector", "Detecting") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap",                  "Bitmap", "Obrázek ve tvaru èerná=pozadí, bílá=èáry\nBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Mode",                     "Mode",   "Pøedbìné hlenání èar (od 0 do 1, vìtší èíslo lepší)\nPresearch, 0 fast and unprecise to 1 better quality", GH_ParamAccess.item, 0.95);
            pManager.AddNumberParameter("Thickness",                "Number", "Tlouška èar\nLine thickness", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Connect others",           "Number", "Pøipojení k ostatním èarám\nConnect other polylines, bigger number connect more far ones", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Closing self",             "Number", "Uzavírání vlatní polyline\nConnect start of specific polyline to his end, bigger number connect more far ones", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Connect to body",          "Number", "Pøipojení k tìlu polyline (Vhodné na tvary typu '8')\nConnect other polylines body, bigger number connect more far ones", GH_ParamAccess.item, 4);
            pManager.AddNumberParameter("Simplify",                 "Number", "Zjednodušit vıstup (0=nezjednodušovat)\nSimplify polyline, begger more simplify, 0=no symplify. Ramer–Douglas–Peucker algorithm - eta value", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Ignore pixels presearch",  "Number", "Ingnorovat nízké pixely v pøedbìném hlenání (0=neignorovat)\nPresearch ignore low pixel value (1=no ignore .. to .. 255=max ignore)", GH_ParamAccess.item, 50);
            pManager.AddNumberParameter("Ignore pixels",            "Number", "Ingnorovat nízké pixely (0=neignorovat)\nIgnore low pixel value (1=no ignore .. to .. 255=max ignore)", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Only closed",             "Boolean", "Pouze uravøené polyline\nGives only closed polylines", GH_ParamAccess.item, false);

            // Pozice a velikost
            pManager.AddPointParameter("Point", "Position", "Pozice umístìní\nPosition", GH_ParamAccess.item, new Point3d(0,0,0));
            pManager.AddVectorParameter("Vector", "Scale", "velikost vıstupu, záporná znamená pøevrátit\nScale, flip, (z - don't care)", GH_ParamAccess.item, new Vector3d(1,1,0));

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Curves", "Curves", "Køivky\nCurves (curve can be line)", GH_ParamAccess.list);  
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {

            // nastavit promìnné
            Bitmap bitmapOriginal = null;
            double RawTickness = 3.0;
            double quality=0.5;
            double RawConnectToOthers=1.0;
            double RawConnectToSelf=1.0;
            double RawConnectToBody=1.0;
            double RawSimplify=1.0;
            double RawIgnore=50;
            double RawIgnore2=5;
            bool ClosedOnly=false;
            Vector3d TransformVector = new Vector3d(1,1,0);
            Point3d TransformLocation = new Point3d(0,0,0);
            int tickness;

            // naèíst
            if (!DA.GetData(0, ref bitmapOriginal)) {
                DA.AbortComponentSolution();
                return;
            } 
            
            // Rozmìry obrázku
            int PictureWidth=bitmapOriginal.Width, PictureHeight=bitmapOriginal.Height;  
            Rectangle rec = new Rectangle(0, 0, PictureWidth, PictureHeight);
            Bitmap bitmap = bitmapOriginal.Clone(rec, PixelFormat.Format24bppRgb);

            // Tlouška èar
            if (DA.GetData(2, ref RawTickness)) {
                if (RawTickness<=0) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,"Tickness should be bigger than zero");
                    DA.AbortComponentSolution();
                    return;
                }
            }
            tickness=(int)(RawTickness+0.5f);

            // Pøipojení k ostatním
            DA.GetData(3, ref RawConnectToOthers);
            float connectToOthers=(float)RawConnectToOthers;

            // Pøipojení sebe
            DA.GetData(4, ref RawConnectToSelf);
            float connectConnectToSelf=(float)RawConnectToSelf;

            // Pøipojení k tìlu
            DA.GetData(5, ref RawConnectToBody);
            float connectToBody=(float)RawConnectToBody; 
            
            // Zjednodušení
            DA.GetData(6, ref RawSimplify);
            float simplify=(float)RawSimplify;

            // Ignorování pro pøedbìné
            DA.GetData(7, ref RawIgnore);
            int ignore=(int)RawIgnore;
            if (ignore<1)ignore=1;
            if (ignore>255)ignore=255;

            // Ignorování pro detekci èar
            DA.GetData(8, ref RawIgnore2);
            int ignore2=(int)RawIgnore2;
            if (ignore2<1)ignore2=1;
            if (ignore2>255)ignore2=255;
            DA.GetData(9, ref ClosedOnly);

            // Pozice v projektu
            DA.GetData(10, ref TransformLocation);
            DA.GetData(11, ref TransformVector);

            // Tlouška èar v obrázku 
            PacMan.DistanceMin=tickness;
            PacMan.DistanceMax=tickness+1;
            PacMan.DistanceMinD=PacMan.DistanceMin;
            PacMan.DistanceMaxD=PacMan.DistanceMax;

            // Kvalita pøedbìného hledání
            if (!DA.GetData(1, ref quality)) {
                quality=0.5;
            }
           
            // Pacman má znát rozmìry obrázku
            PacMan.PictureHeight=PictureHeight;
            PacMan.PictureWidth=PictureWidth;
                      
            BitmapData data = bitmap.LockBits(rec, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            // Pozice obrázku v pamìti
            byte* pointer = (byte*)data.Scan0;
            PacMan.Pointer=pointer;

            // Šíøka obrázku*3
            PacMan.Stride=data.Stride;

            // vyèisti obrázek, jeden kanál zachovat, jeden k oznaèování kde u se detekce øešila, jeden a zní práznou
            {
                byte* pointerClean=pointer+1;
                int to=rec.Width*rec.Height*3;
                byte* pointerCleanTo=pointerClean+to;
                for (; pointerClean<pointerCleanTo; pointerClean+=3) *pointerClean = 0;

                // Zbyteèné, moná pro dev
                pointerClean=pointer+2;
                for (; pointerClean<pointerCleanTo; pointerClean+=3) *pointerClean = 0;
            }

            // Pøedbìné analyzování potøebuje rozmìry obrázku
            Analyzator.PictureWidth=PictureWidth;
            Analyzator.PictureHeight=PictureHeight;
            Analyzator.Stride=data.Stride;
            Analyzator.Pointer=pointer;

            // Je menší šíøka nebo vıška?
            int min=1, max;
            if (PictureWidth<PictureHeight) max=bitmap.Height/2;
            else max=bitmap.Width/2;

            // Vzálenosti pro pøedbìné hledání
            Analyzator.NextPoint=(int)(min+(max-min)*(1-quality));
            List<DInt> PreAnalyzed=Analyzator.Analyze(ignore);
           
            // Vıpoèet pixelové vzdálenosti pro napojování
            float limitDistanceToConnectToOthers= tickness*connectToOthers ;
            float limitDistanceToConnectToSelf  = tickness*connectConnectToSelf;
            float limitDistanceToConnectToBody  = tickness*connectToBody;

            List<MyPolyline> polylines=new List<MyPolyline>();
            foreach (DInt pt in PreAnalyzed) {
                
                // Ze støedu na levo a na pravo  <<-<X>->>
                MyPolyline 
                    left=RunPackMann(), 
                    right=RunPackMann();

                MyPolyline RunPackMann(){
                    PacMan packMan = new PacMan(pt.X, pt.Y);

                    List<DIntB> Points = new List<DIntB>();
                    for (int i = 0; i < 1000; i++) {

                        // Kde by ses mohl pøesunout
                        TInt item = packMan.FindNextPoint(i==0);

                        // Není kde se pøesonout
                        if (item.X == -1) {
                            break;
                        }

                        // Nemìlo by nastat, ale co u vezkoušé
                        if (item.X==packMan.currentPosX && item.Y==packMan.currentPosY) {

                            // Zkus odìlat smeèko
                            if (Points.Count > 7 && Points.Count>(int)limitDistanceToConnectToSelf+1) {
                                DIntB start = Points[0];
                                int dX = start.X - item.X, dY = start.Y - item.Y;
                                double dis = Math.Sqrt(dX*dX + dY*dY);
                                if (dis < limitDistanceToConnectToSelf/3) {
                                    if (Points[0].X==item.X && Points[0].Y==item.Y){ 
                                        Points[0].SetN(true);
                                        Points[Points.Count-1].SetN(true);
                                    }else{
                                        Points.Add(new DIntB(start.X, start.Y, true));
                                        Points[0].SetN(true);
                                    }
                                    break;
                                }
                            }
                            break;
                        }

                        if (item.N>0 && item.N<ignore2) break;

                        // Packmann: Tady jsem u byl, další to tady øešit nebudou 
                        // Zaplò aktuálni pozico
                        PacMan.SetValueArea1(packMan.currentPosX, packMan.currentPosY, tickness-1, 1);

                        if (Points.Count > 2) {
                            DIntB last=Points[Points.Count - 1];
                            DIntB prevLast=Points[Points.Count - 2];
                            int x = (last.X + prevLast.X) / 2;
                            int y = (last.Y + prevLast.Y) / 2;
                            PacMan.SetValueArea1(x, y, tickness, 1);
                        }
                                            
                        // Mezi dvìmi body souèastnım a minulım, oznaè e tady byl
                        if (Points.Count==3) { 
                            PacMan.SetValueArea2(Points[0].X, Points[0].Y, tickness, 2);
                            PacMan.SetValueArea1(Points[1].X, Points[1].Y, tickness, 0);

                            DIntB last=Points[0];
                            DIntB prevLast=Points[1];
                            int x = (last.X + prevLast.X) / 2;
                            int y = (last.Y + prevLast.Y) / 2;
                            PacMan.SetValueArea1(x, y, tickness, 1);
                        }

                        // Pøipojování k zaèátku polyline
                        if (Points.Count > 7 && Points.Count>(int)limitDistanceToConnectToSelf+1) {
                            DIntB start = Points[0];
                            
                            // Vzdálenost zaèátku a souèastného bodu
                            int dX = start.X - item.X, dY = start.Y - item.Y;
                            double dis = Math.Sqrt(dX*dX + dY*dY);

                            
                            if (dis < limitDistanceToConnectToSelf/3) {
                                Points.Add(new DIntB(start.X, start.Y, true));
                                Points[0].SetN(true);
                                break;
                            }
                        }

                        // Spojit zaèátek a konec
                        if (item.N==-2) { 
                            PacMan.SetValueArea2(Points[0].X, Points[0].Y, tickness, 1);
                            Points.Add(new DIntB(Points[0].X, Points[0].Y,true));
                            Points[0].SetN(true);
                            break;
                        }

                        // Pøesuò packmanna na novou pozicu
                        Points.Add(new DIntB(item.X, item.Y,false));
                        packMan.SetPosition(item.X, item.Y);
                    }
                
                    // Samoté bod nemá ani cenu øešit
                    if (Points.Count == 0) return null;
                    if (Points.Count == 1) { 
                        DIntB p=Points[0];
                        return null;
                    }

                    PacMan.SetValueArea2(Points[0].X, Points[0].Y, tickness, 1);
                    MyPolyline polyline=new MyPolyline(Points);
                    return polyline;
                }
                
                // Propoj na pravo a na levo  E<<<<SXS>>>>E  =>   S>>>>X>>>>E
                if (left!=null && right!=null) { 
                    // Spoj polyline vycházejícího z jedného pøedbìného místa
                    if (left.TryMergeWith(right)) { 
                        polylines.Add(left);
                        PacMan.SetValueArea1(left.Points[0].X, left.Points[0].Y, tickness+1, 1);
                        PacMan.SetValueArea1(right.Points[0].X, right.Points[0].Y, tickness+1, 1);
                    } else { 
                        // Poèítat s toutou polyline 
                        polylines.Add(left);
                        polylines.Add(right);
                    }                    
                } else { 
                    MyPolyline polyline;

                    if ((polyline=left!=null ? polyline=left : (right!=null) ? polyline=right : null) != null) {
                        // Try close self
                        if (!polyline.TryCloseSelf(limitDistanceToConnectToSelf, (int)connectConnectToSelf+2)) PacMan.SetValueArea1(polyline.Points[0].X, polyline.Points[0].Y, tickness, 1);
                        
                        // Poèítat s toutou polyline 
                        polylines.Add(polyline);
                    } 
                }
            }            
                      

            // Connect polylines
            // Zkus napojit na jinou smyèku jak na vlastni
            foreach (MyPolyline pFrom in polylines) {                 
                if (pFrom.Start && pFrom.End) continue;

                DIntB end=pFrom.Points[pFrom.Points.Count-1];

                // Nejlepší indexy kde pøipojit
                int nearestS=-1,
                    nearestE=-1;

                // Nejlepší vzdálenosti-nejkratší
                double 
                    disEndToStart=0,
                    disEndToEnd=0;                    

                for (int j=0; j<polylines.Count; j++) {
                    MyPolyline pTo = polylines[j];

                    // Zkus pøipojit konec a zaèátek
                    if (!pTo.Start) {
                        DIntB Tstart = pTo.Points[0];

                        // Vzdálenost jedné polyline konce a zaèátku druhé polyline
                        int dX = Tstart.X - end.X, 
                            dY = Tstart.Y - end.Y;
                        double Tdis = Math.Sqrt(dX*dX + dY*dY);

                        // Jesli je moné bod pøipojit
                        if (Tdis <= limitDistanceToConnectToOthers) {
                            if (Tdis<disEndToStart) { 
                                nearestS=j; 
                                disEndToStart=Tdis; 
                            }
                        }
                    } 

                    // Zkus pøipojit koncovı bod
                    if (!pTo.End) {
                        
                        DIntB Tend=pTo.Points[pTo.Points.Count-1];

                        // Vzdálenost jedné polyline konce a zaèátku druhé polyline
                        int dX=Tend.X-end.X, 
                            dY=Tend.Y-end.Y;
                        double Tdis2=Math.Sqrt(dX*dX + dY*dY);

                        // Jesli je moné bod pøipojit
                        if (Tdis2<=limitDistanceToConnectToOthers) {
                            if (Tdis2<disEndToEnd) { 
                                nearestE=j;
                                disEndToEnd=Tdis2; 
                            }
                        }
                    }                       
                }
                
                // konec se nepøipojije ke konci
                if (pFrom.End) continue;

                // Jak spojovat polyline
                if (nearestS!=-1 && nearestE!=-1) { 
                    if (disEndToStart<disEndToEnd) {
                        CTS();
                    } else {
                        CTE();
                    }
                } else if (nearestS!=-1) { 
                    CTS();
                } else if (nearestS!=-1) { 
                    CTE();
                }

                // Funce s pøipojením na zaèátek
                void CTS() {                    
                    MyPolyline pl=polylines[nearestS];
                    DIntB Tstart=pl.Points[0];                   
                    pl.Points[0].SetN(true);

                    pFrom.AddClosingEndPoint(Tstart);
                }

                // Funce s pøipojením na konec
                void CTE() {
                    MyPolyline pl=polylines[nearestE];
                    DIntB Tend=pl.Points[pl.Points.Count-1];

                    pFrom.AddClosingEndPoint(Tend);
                    pl.End=true;
                }    
            }
               
            // Zkus napojit na svou smyèku, nekde doprostøed
            foreach (MyPolyline polyline in polylines) {  
                // Uzavøená smyèka, neøeš 
                if (polyline.Start && polyline.End) continue;

                // Pøipoj svou smyèku od nìkuï k zaèátku
                if (!polyline.Start) { 
                    int result = polyline.NearPointToSelfPointStart(limitDistanceToConnectToBody, (int)connectToBody+1);

                    if (result!=-1) { 
                        DIntB pt=polyline.Points[result];
                        polyline.Points[result].SetN(true);
                        polyline.AddClosingStartPoint(pt);
                    }
                } 
                
                // Pøipoj svou smyèku od nìkuï ke konci
                if (!polyline.End) { 
                    int result = polyline.NearPointToSelfPointEnd(limitDistanceToConnectToBody, (int)connectToBody+1);

                    if (result!=-1) { 
                        DIntB pt=polyline.Points[result];
                        polyline.AddClosingEndPoint(pt);
                    }
                }
            }

            // Zkus napojit na jinou smyèku, nekde doprostøed
            foreach (MyPolyline polyline in polylines) {  
                // Uzavøená smyèka, neøeš 
                if (polyline.Start && polyline.End) continue;

                // Pøipoj nìkde zaèátek
                if (!polyline.Start) { 
                    DIntB start=polyline.Points[0];

                    (MyPolyline, int) result = MyPolyline.NearPointToPoint(polylines, limitDistanceToConnectToBody, start, polyline);
                    if (result.Item1!=null) { 
                        DIntB pt=result.Item1.Points[result.Item2];    
                        result.Item1.Points[result.Item2].SetN(true); // Oznam e je pøipojené
                        polyline.AddClosingStartPoint(pt);
                    }
                } 
                
                // Pøipoj nìkde konec
                if (!polyline.End) { 
                    DIntB end=polyline.Points[polyline.Points.Count-1];

                    (MyPolyline, int) result = MyPolyline.NearPointToPoint(polylines, limitDistanceToConnectToBody, end, polyline);
                    if (result.Item1!=null) { 
                        DIntB pt=result.Item1.Points[result.Item2];
                        result.Item1.Points[result.Item2].SetN(true); // Oznam e je pøipojené
                        polyline.AddClosingEndPoint(pt);
                    }
                }
            }
             
            // Optimalizace stejnıch bodù - nemìla byt èastá 
            foreach (MyPolyline pol in polylines) { 
                DIntB lastPoint=pol.Points[0];

                for (int i = 1; i < pol.Points.Count; i++) {
                    DIntB pt=pol.Points[i];
                    if (pt.X==lastPoint.X && pt.Y==lastPoint.Y) { 

                        // Sma stejné body, zkontrolovat jestli jsou pøipojené, pøipojenost zachovat
                        if (lastPoint.N && pt.N) {
                            pol.Points.RemoveAt(i);
                            i--;
                            continue;
                        } else if (lastPoint.N && !pt.N) {
                            pol.Points.RemoveAt(i);
                            i--;     
                            continue;                       
                        } else if (!lastPoint.N && pt.N) {
                            pol.Points.RemoveAt(i-1);
                            i--;    
                            continue;                        
                        } else if (!lastPoint.N && !pt.N) {
                            pol.Points.RemoveAt(i);
                            i--;   
                            continue;
                        }
                    }
                    lastPoint=pt;
                }
            }

            // Konvertuj pro vıstup
            List<Polyline> pls2 = new List<Polyline>();
            foreach (MyPolyline pol in polylines) {  
                // Pøi pouze uzavøenıch neodesílat nepøipojené
                if (ClosedOnly) {
                    if (!pol.Start || !pol.End) continue;
                }

                int len=pol.Points.Count;
                List<Point3d> pnts3d = new List<Point3d>(){ Capacity=len }; 
            
                // Zjednodušení polyline     
                DIntB[] sympfifiedPoints;                    
                if (simplify==0) sympfifiedPoints=pol.Points.ToArray();
                else sympfifiedPoints=SimplifyPolyline.Reduce(pol.Points.ToArray(), simplify);

                // Nemìlo by nastat, kdy nastave tak nezjednušuj
                if (sympfifiedPoints.Length<=2) sympfifiedPoints=pol.Points.ToArray();

                // Pøeveï body z DIntB na Point3d
                for (int i = 0; i < sympfifiedPoints.Length; i++) {
                    DIntB pt=sympfifiedPoints[i];
                    pnts3d.Add(new Point3d(pt.X*TransformVector.X+TransformLocation.X, pt.Y*TransformVector.Y+TransformLocation.Y, 0));
                }

                // Zapsat vısledek
                pls2.Add(new Polyline(pnts3d)); 
            }
            
            bitmap.UnlockBits(data);

            try {
                DA.SetDataList(0, pls2);
            } catch {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Nepodaøilo se odeslat data do Grasshopperu\nCannot send data to Grasshopper");
                DA.AbortComponentSolution();
            }
        }
                
        protected override Bitmap Icon => Properties.Resources.detect;

        public override Guid ComponentGuid => new Guid("44712965-93F3-43EE-BDC4-3257A082F521");
    }
}