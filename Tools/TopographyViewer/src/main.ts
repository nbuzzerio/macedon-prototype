import './style.css';
import { calculateSlopeDegrees, contourLevels, crossesContour, decodePayload, fitToView, localElevation, parseMetadata, sampleIndex, worldElevation, type Dataset, type TopographyMetadata, type ViewTransform } from './topography';

document.querySelector<HTMLDivElement>('#app')!.innerHTML = `
  <header><div><span class="eyebrow">MACEDON WORLD TOOLS</span><h1>Terrain Topography</h1></div><div id="dataset-name">No dataset loaded</div></header>
  <main>
    <aside>
      <label class="file-button">Load export <input id="files" type="file" accept=".json,.raw,application/json" multiple></label>
      <p class="hint">Choose the matching <code>.topography.json</code> and <code>.height.raw</code> files together, or drop both on the map.</p>
      <section><h2>Layers</h2>
        <label><input id="height" type="checkbox" checked> Height color</label>
        <label><input id="contours" type="checkbox" checked> Contour lines</label>
        <label><input id="hillshade" type="checkbox" checked> Hillshade</label>
        <label><input id="slope" type="checkbox"> Steep-slope overlay</label>
      </section>
      <section><h2>Contours</h2>
        <label class="stack">Interval <span><input id="interval" type="number" value="5" min="0.1" step="0.5"> m</span></label>
        <label class="stack">Major every <span><input id="major" type="number" value="5" min="1" step="1"> lines</span></label>
      </section>
      <section><h2>Relief</h2>
        <label class="stack">Light azimuth <span><input id="azimuth" type="range" value="315" min="0" max="360"><output id="azimuth-value">315°</output></span></label>
        <label class="stack">Steep threshold <span><input id="threshold" type="range" value="35" min="5" max="70"><output id="threshold-value">35°</output></span></label>
      </section>
      <button id="fit" type="button">Fit terrain</button>
      <div id="metadata" class="metadata"></div>
    </aside>
    <div id="stage" tabindex="0"><canvas id="map"></canvas><div id="drop-message">Drop JSON + RAW export here</div><div id="coordinates">Move over terrain for coordinates</div><div class="axis"><b>+Z</b><i>↑</i><span>+X →</span></div></div>
  </main>`;

const canvas = document.querySelector<HTMLCanvasElement>('#map')!;
const stage = document.querySelector<HTMLDivElement>('#stage')!;
const ctx = canvas.getContext('2d')!;
let dataset: Dataset | undefined;
let raster: HTMLCanvasElement | undefined;
let view: ViewTransform = { scale: 1, offsetX: 0, offsetY: 0 };
let dragging = false, lastX = 0, lastY = 0, spaceDown = false;
const controlIds = ['height', 'contours', 'hillshade', 'slope', 'interval', 'major', 'azimuth', 'threshold'];
const input = (id: string) => document.querySelector<HTMLInputElement>(`#${id}`)!;

function resize(): void {
  const ratio = window.devicePixelRatio || 1, rect = stage.getBoundingClientRect();
  canvas.width = Math.round(rect.width * ratio); canvas.height = Math.round(rect.height * ratio);
  canvas.style.width = `${rect.width}px`; canvas.style.height = `${rect.height}px`;
  ctx.setTransform(ratio, 0, 0, ratio, 0, 0); render();
}

async function loadFiles(files: File[]): Promise<void> {
  const json = files.find(file => file.name.endsWith('.json'));
  if (!json) throw new Error('Select the .topography.json metadata file.');
  const metadata = parseMetadata(JSON.parse(await json.text()));
  const raw = files.find(file => file.name === metadata.heightData.file) ?? files.find(file => file.name.endsWith('.raw'));
  if (!raw) throw new Error(`Select companion payload ${metadata.heightData.file}.`);
  dataset = { metadata, normalized: decodePayload(metadata, await raw.arrayBuffer()) };
  buildRaster(); fit(); updateMetadata();
  document.querySelector('#drop-message')!.classList.add('hidden');
}

function buildRaster(): void {
  if (!dataset) return;
  const { metadata: m, normalized: heights } = dataset, n = m.heightmapResolution;
  raster = document.createElement('canvas'); raster.width = n; raster.height = n;
  const rasterCtx = raster.getContext('2d')!, image = rasterCtx.createImageData(n, n);
  const min = m.minimumNormalizedHeight, span = Math.max(1e-9, m.maximumNormalizedHeight - min);
  const azimuth = Number(input('azimuth').value) * Math.PI / 180;
  for (let screenY = 0; screenY < n; screenY++) {
    const z = n - 1 - screenY;
    for (let x = 0; x < n; x++) {
      const normalized = heights[sampleIndex(x, z, n)], t = (normalized - min) / span;
      let [r, g, b] = input('height').checked ? terrainColor(t) : [151, 155, 157];
      if (input('hillshade').checked) {
        const x0 = Math.max(0, x - 1), x1 = Math.min(n - 1, x + 1), z0 = Math.max(0, z - 1), z1 = Math.min(n - 1, z + 1);
        const dx = (heights[sampleIndex(x1,z,n)] - heights[sampleIndex(x0,z,n)]) * m.sizeMeters.y / ((x1-x0)*m.sizeMeters.x/(n-1));
        const dz = (heights[sampleIndex(x,z1,n)] - heights[sampleIndex(x,z0,n)]) * m.sizeMeters.y / ((z1-z0)*m.sizeMeters.z/(n-1));
        const length = Math.hypot(-dx, 1, -dz), lx = Math.sin(azimuth)*.707, lz = Math.cos(azimuth)*.707;
        const light = Math.max(-.35, Math.min(1, (-dx*lx + .707 + -dz*lz) / length));
        const factor = .7 + .48 * light; r *= factor; g *= factor; b *= factor;
      }
      if (input('slope').checked) {
        const slope = calculateSlopeDegrees(heights, m, x, z), threshold = Number(input('threshold').value);
        if (slope > threshold) { const amount = Math.min(.78, (slope-threshold)/30+.2); r = r*(1-amount)+245*amount; g *= 1-amount; b *= 1-amount; }
      }
      const p = (screenY*n+x)*4; image.data[p]=r; image.data[p+1]=g; image.data[p+2]=b; image.data[p+3]=255;
    }
  }
  rasterCtx.putImageData(image, 0, 0); render();
}

function terrainColor(t: number): [number, number, number] {
  const stops: [number, number, number, number][] = [[0,31,74,91],[.18,53,111,92],[.42,139,142,83],[.68,170,126,85],[.84,181,165,137],[1,239,240,232]];
  const upper = stops.findIndex(stop => stop[0] >= t); if (upper <= 0) return stops[0].slice(1) as [number,number,number];
  const a=stops[upper-1], b=stops[upper], u=(t-a[0])/(b[0]-a[0]); return [a[1]+(b[1]-a[1])*u,a[2]+(b[2]-a[2])*u,a[3]+(b[3]-a[3])*u];
}

function render(): void {
  const ratio=window.devicePixelRatio||1, width=canvas.width/ratio, height=canvas.height/ratio;
  ctx.clearRect(0,0,width,height); if (!dataset || !raster) return;
  const m=dataset.metadata, mapW=m.sizeMeters.x*view.scale, mapH=m.sizeMeters.z*view.scale;
  ctx.imageSmoothingEnabled=true; ctx.drawImage(raster,view.offsetX,view.offsetY,mapW,mapH);
  if (input('contours').checked) drawContours(m);
  ctx.strokeStyle='rgba(255,255,255,.45)'; ctx.lineWidth=1; ctx.strokeRect(view.offsetX,view.offsetY,mapW,mapH);
}

function drawContours(m: TopographyMetadata): void {
  if (!dataset) return; const n=m.heightmapResolution, interval=Number(input('interval').value); if (!(interval>0)) return;
  const levels=contourLevels(m.minimumWorldElevation,m.maximumWorldElevation,interval), major=Math.max(1,Math.round(Number(input('major').value)));
  for (let li=0;li<levels.length;li++) {
    const level=(levels[li]-m.worldOrigin.y)/m.sizeMeters.y; ctx.beginPath();
    for(let z=0;z<n-1;z++) for(let x=0;x<n-1;x++) {
      const v=[dataset.normalized[sampleIndex(x,z,n)],dataset.normalized[sampleIndex(x+1,z,n)],dataset.normalized[sampleIndex(x+1,z+1,n)],dataset.normalized[sampleIndex(x,z+1,n)]];
      const points:[number,number][]=[]; edge(points,x,z,x+1,z,v[0],v[1],level,n); edge(points,x+1,z,x+1,z+1,v[1],v[2],level,n); edge(points,x+1,z+1,x,z+1,v[2],v[3],level,n); edge(points,x,z+1,x,z,v[3],v[0],level,n);
      for(let p=0;p+1<points.length;p+=2){ctx.moveTo(points[p][0],points[p][1]);ctx.lineTo(points[p+1][0],points[p+1][1]);}
    }
    const isMajor=li%major===0; ctx.strokeStyle=isMajor?'rgba(18,20,20,.82)':'rgba(21,24,23,.46)'; ctx.lineWidth=isMajor?1.35:.65; ctx.stroke();
  }
}

function edge(points:[number,number][], ax:number,az:number,bx:number,bz:number,a:number,b:number,level:number,n:number):void {
  if(!crossesContour(a,b,level)) return; const t=(level-a)/(b-a), localX=(ax+(bx-ax)*t)/(n-1)*dataset!.metadata.sizeMeters.x, localZ=(az+(bz-az)*t)/(n-1)*dataset!.metadata.sizeMeters.z;
  points.push([view.offsetX+localX*view.scale,view.offsetY+(dataset!.metadata.sizeMeters.z-localZ)*view.scale]);
}

function fit():void { if(!dataset)return; const r=stage.getBoundingClientRect(); view=fitToView(r.width,r.height,dataset.metadata.sizeMeters.x,dataset.metadata.sizeMeters.z,44); render(); }
function updateMetadata():void { if(!dataset)return; const m=dataset.metadata; document.querySelector('#dataset-name')!.textContent=m.terrainName; document.querySelector('#metadata')!.innerHTML=`<b>${m.heightmapResolution} × ${m.heightmapResolution}</b><span>${m.sizeMeters.x.toFixed(0)} × ${m.sizeMeters.z.toFixed(0)} m</span><span>World elevation ${m.minimumWorldElevation.toFixed(1)}–${m.maximumWorldElevation.toFixed(1)} m</span>`; }

function pointerCoordinates(event:PointerEvent):void {
  if(!dataset)return; const m=dataset.metadata, localX=(event.offsetX-view.offsetX)/view.scale, localZ=m.sizeMeters.z-(event.offsetY-view.offsetY)/view.scale;
  if(localX<0||localZ<0||localX>m.sizeMeters.x||localZ>m.sizeMeters.z){document.querySelector('#coordinates')!.textContent='Outside terrain';return;}
  const sx=Math.round(localX/m.sizeMeters.x*(m.heightmapResolution-1)), sz=Math.round(localZ/m.sizeMeters.z*(m.heightmapResolution-1)), normalized=dataset.normalized[sampleIndex(sx,sz,m.heightmapResolution)], localY=localElevation(normalized,m), slope=calculateSlopeDegrees(dataset.normalized,m,sx,sz);
  document.querySelector('#coordinates')!.textContent=`Local X ${localX.toFixed(1)}  Z ${localZ.toFixed(1)}  •  World X ${(m.worldOrigin.x+localX).toFixed(1)}  Z ${(m.worldOrigin.z+localZ).toFixed(1)}  •  Elevation ${localY.toFixed(1)} local / ${worldElevation(localY,m).toFixed(1)} world  •  Slope ${slope.toFixed(1)}°`;
}

input('files').addEventListener('change',e=>loadFiles(Array.from((e.target as HTMLInputElement).files??[])).catch(showError));
stage.addEventListener('dragover',e=>{e.preventDefault();stage.classList.add('dragover')}); stage.addEventListener('dragleave',()=>stage.classList.remove('dragover')); stage.addEventListener('drop',e=>{e.preventDefault();stage.classList.remove('dragover');loadFiles(Array.from(e.dataTransfer?.files??[])).catch(showError)});
stage.addEventListener('pointerdown',e=>{if(e.button===1||e.button===0||spaceDown){dragging=true;lastX=e.clientX;lastY=e.clientY;stage.setPointerCapture(e.pointerId)}}); stage.addEventListener('pointerup',()=>dragging=false); stage.addEventListener('pointermove',e=>{pointerCoordinates(e);if(dragging){view.offsetX+=e.clientX-lastX;view.offsetY+=e.clientY-lastY;lastX=e.clientX;lastY=e.clientY;render()}});
stage.addEventListener('wheel',e=>{e.preventDefault();const factor=Math.exp(-e.deltaY*.001),x=e.offsetX,y=e.offsetY;view.offsetX=x-(x-view.offsetX)*factor;view.offsetY=y-(y-view.offsetY)*factor;view.scale*=factor;render()},{passive:false});
window.addEventListener('keydown',e=>{if(e.code==='Space')spaceDown=true});window.addEventListener('keyup',e=>{if(e.code==='Space')spaceDown=false});
document.querySelector('#fit')!.addEventListener('click',fit); for(const id of controlIds) input(id).addEventListener('input',()=>{document.querySelector(`#${id}-value`)?.replaceChildren(`${input(id).value}°`);buildRaster()});
new ResizeObserver(resize).observe(stage); function showError(error:unknown):void{document.querySelector('#drop-message')!.classList.remove('hidden');document.querySelector('#drop-message')!.textContent=error instanceof Error?error.message:String(error)}
