using System;

namespace DetectLinesInPicture {
    unsafe class PackMan {
        public int currentPosX;
        public int currentPosY;
        
        public static byte* Pointer;
        public static int PictureWidth;
        public static int PictureHeight;
        public static int DistanceMin, DistanceMax;
        public static double DistanceMinD, DistanceMaxD;

        public static int Stride;

        public PackMan() { }
        public PackMan(int x, int y) { 
            SetPosition(x,y);    
        }

        public void SetPosition(int x, int y) { 
            currentPosX=x;
            currentPosY=y;
        }
        
        public TInt FindNextPoint(bool first) { 
            TInt Best=new TInt(-1, -1, 0);

            for (int iy=-DistanceMax; iy<=DistanceMax; iy++) {
                for (int ix=-DistanceMax; ix<=DistanceMax; ix++) { 
                    double dis=Math.Sqrt(ix*ix+iy*iy);
                    if ((dis <= DistanceMax && dis>DistanceMin) || (first && dis <= DistanceMax+1/* && dis>DistanceMin*/)) {
                        int posX=currentPosX+ix,
                            posY=currentPosY+iy;

                        if (posX<0) continue;
                        if (posY<0) continue;
                        if (posX>=PictureWidth) continue;
                        if (posY>=PictureHeight) continue;

                        // Is not already used
                        float valueA=GetValue(posX, posY, 1);
                        if (valueA==1) continue;
                       
                        
                        // best value lead
                        int valueN=GetValue(posX, posY, 0);
                        if (valueN==0) continue;
                        if (valueA==2) { 
                             TInt tint=new TInt{ X=posX, Y=posY, N=-2 };
                        
                            if (Best.N<tint.N) Best=tint;
                        } else {
                            TInt tint=new TInt{ X=posX, Y=posY, N=valueN };
                        
                            if (Best.N<tint.N) Best=tint;
                        }
                    }
                }
            }
            return Best;
        }

        public int GetValue(int x, int y, int ch) => *(Pointer + y*Stride+x*3+ch);

        public void SetValue(int x, int y, int ch, byte value) => *(Pointer + Stride*y+x*3+ch)=value;
       
        int GetValueArea(int x, int y, int radius) { 
            int ch=0;
            int value=0;

            for (int ix=-radius; ix<=radius; ix++) { 
                for (int iy=-radius; iy<=radius; iy++) {
                    double dis=Math.Sqrt(ix*ix+iy*iy);
                    if (dis <= radius) {
                        int posX=x+ix;
                        int posY=y+iy;
                        if (posX<0) continue;
                        if (posY<0) continue;
                        if (posX>PictureWidth) continue;
                        if (posY>PictureWidth) continue;
                        value+=GetValue(x,y,ch);
                    }
                }
            }
            return value;
        }

        public static int SetValueArea(int x, int y, int radius, byte value) { 
          //  int ch=1;
            
            for (int ix=-radius; ix<=radius; ix++) { 
                for (int iy=-radius; iy<=radius; iy++) {
                    double dis=Math.Sqrt(ix*ix+iy*iy);
                    if (dis <= radius) {
                        int posX=x+ix;
                        int posY=y+iy;
                        if (posX<0) continue;
                        if (posY<0) continue;
                        if (posX>=PictureWidth) continue;
                        if (posY>=PictureHeight) continue;
                      //  SetValue(posX,posY,ch,value);
                        *(Pointer + Stride*posY+posX*3+1)=value;
                    }
                }
            }
            return value;
        }

         public static int SetValueArea2(int x, int y, int radius, byte value) { 
          //  int ch=1;
            
            for (int ix=-radius; ix<=radius; ix++) { 
                for (int iy=-radius; iy<=radius; iy++) {
                    double dis=Math.Sqrt(ix*ix+iy*iy);
                    if (dis <= radius) {
                        int posX=x+ix;
                        int posY=y+iy;
                        if (posX<0) continue;
                        if (posY<0) continue;
                        if (posX>=PictureWidth) continue;
                        if (posY>=PictureHeight) continue;
                      //  SetValue(posX,posY,ch,value);
                        *(Pointer + Stride*posY+posX*3+2)=value;
                    }
                }
            }
            return value;
        }
    }
}