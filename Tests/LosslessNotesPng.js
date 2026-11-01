/* Re-encode native RGBA screenshots losslessly; never change pixel values. */
'use strict';
const fs=require('fs'),zlib=require('zlib'),crypto=require('crypto');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function paeth(a,b,c){const p=a+b-c,aa=Math.abs(p-a),bb=Math.abs(p-b),cc=Math.abs(p-c);return aa<=bb&&aa<=cc?a:bb<=cc?b:c;}
function read(buffer){
 if(buffer.subarray(0,8).toString('hex')!=='89504e470d0a1a0a')throw Error('PNG required');
 const chunks=[],idat=[];
 for(let pos=8;pos<buffer.length;){const n=buffer.readUInt32BE(pos),type=buffer.toString('ascii',pos+4,pos+8);if(type==='IDAT')idat.push(buffer.subarray(pos+8,pos+8+n));else chunks.push({type,bytes:buffer.subarray(pos,pos+n+12)});pos+=n+12;}
 const width=buffer.readUInt32BE(16),height=buffer.readUInt32BE(20);
 if(buffer[24]!==8||![2,6].includes(buffer[25])||buffer[28]!==0)throw Error('Only native noninterlaced 8-bit RGB/RGBA is supported');
 const channels=buffer[25]===6?4:3,stride=width*channels,raw=zlib.inflateSync(Buffer.concat(idat)),pixels=Buffer.alloc(stride*height);
 if(raw.length!==(stride+1)*height)throw Error('Unexpected PNG length');
 for(let y=0;y<height;y++)for(let x=0;x<stride;x++){
  const at=y*stride+x,f=raw[y*(stride+1)],a=x>=channels?pixels[at-channels]:0,b=y?pixels[at-stride]:0,c=y&&x>=channels?pixels[at-stride-channels]:0;
  if(f>4)throw Error('Invalid PNG filter');
  pixels[at]=(raw[y*(stride+1)+1+x]+[0,a,b,Math.floor((a+b)/2),paeth(a,b,c)][f])&255;
 }
 let rgba=pixels;if(channels===3){rgba=Buffer.alloc(width*height*4);for(let i=0,j=0;i<pixels.length;i+=3,j+=4){rgba[j]=pixels[i];rgba[j+1]=pixels[i+1];rgba[j+2]=pixels[i+2];rgba[j+3]=255;}}
 return {width,height,stride,pixels:rgba,chunks};
}
function crc32(buffer){let crc=0xffffffff;for(const byte of buffer){crc^=byte;for(let j=0;j<8;j++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}return (crc^0xffffffff)>>>0;}
function pngChunk(name,data){const type=Buffer.from(name),chunk=Buffer.alloc(data.length+12);chunk.writeUInt32BE(data.length,0);type.copy(chunk,4);data.copy(chunk,8);chunk.writeUInt32BE(crc32(Buffer.concat([type,data])),data.length+8);return chunk;}
for(const file of process.argv.slice(2)){
 const original=fs.readFileSync(file),image=read(original);let channels=3;
 for(let i=3;i<image.pixels.length;i+=4)if(image.pixels[i]!==255){channels=4;break;}
 const height=image.height,stride=image.width*channels,filtered=Buffer.alloc((stride+1)*height);let pixels=image.pixels;
 if(channels===3){pixels=Buffer.alloc(image.width*height*3);for(let i=0,j=0;i<image.pixels.length;i+=4,j+=3){pixels[j]=image.pixels[i];pixels[j+1]=image.pixels[i+1];pixels[j+2]=image.pixels[i+2];}}
 for(let y=0;y<height;y++){
  let best=null,score=Infinity;
  for(let f=0;f<5;f++){
   const row=Buffer.alloc(stride+1);row[0]=f;let sum=0;
   for(let x=0;x<stride;x++){const at=y*stride+x,a=x>=channels?pixels[at-channels]:0,b=y?pixels[at-stride]:0,c=y&&x>=channels?pixels[at-stride-channels]:0,v=(pixels[at]-[0,a,b,Math.floor((a+b)/2),paeth(a,b,c)][f]+256)&255;row[x+1]=v;sum+=Math.abs(v<128?v:v-256);}
   if(sum<score){score=sum;best=row;}
  }
  best.copy(filtered,y*(stride+1));
 }
 const compressed=zlib.deflateSync(filtered,{level:9}),parts=[original.subarray(0,8)];
 for(const chunk of image.chunks){if(chunk.type==='IEND')parts.push(pngChunk('IDAT',compressed));if(chunk.type==='IHDR'){const header=Buffer.from(chunk.bytes.subarray(8,chunk.bytes.length-4));header[9]=channels===3?2:6;parts.push(pngChunk('IHDR',header));}else parts.push(chunk.bytes);}
 const result=Buffer.concat(parts);if(hash(read(result).pixels)!==hash(image.pixels))throw Error('Pixel mismatch');
 if(result.length<original.length)fs.writeFileSync(file,result);
 console.log(JSON.stringify({file,originalBytes:original.length,encodedBytes:fs.statSync(file).size,pixelSha256:hash(image.pixels),pixelsUnchanged:true}));
}
