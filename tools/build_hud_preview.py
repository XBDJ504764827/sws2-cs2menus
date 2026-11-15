#!/usr/bin/env python3
"""Generate a standalone browser approximation of the native Panorama menu.

The preview uses sample data. Validate native rendering/input in CS2 separately.
"""
from pathlib import Path
import html
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
LAYOUT = ROOT / 'workshop/panorama/layout/custom_game/sws2cs2menus/menu.xml'
STYLE = ROOT / 'workshop/panorama/styles/custom_game/sws2cs2menus/menu.css'


def element(node):
    tag = {'Panel': 'div', 'Label': 'span', 'Button': 'button'}[node.tag]
    attrs = ''.join(f' {key}="{html.escape(node.attrib[key], quote=True)}"'
                    for key in ('id', 'class') if key in node.attrib)
    value = node.get('text', '')
    if value.startswith('{s:'):
        value = ''
    return f'<{tag}{attrs}>{html.escape(value)}'+''.join(element(c) for c in node)+f'</{tag}>'


css = STYLE.read_text()
css += '\n' + (STYLE.parent / 'map_thumbnails.css').read_text()
css = css.replace('file://{images}/', '../workshop/panorama/images/')
css = css.replace('flow-children: down;', 'display: flex; flex-direction: column;')
css = css.replace('flow-children: right;', 'display: flex; flex-direction: row;')
css = css.replace('width: fill-parent-flow(1);', 'flex: 1; min-width: 0; width: auto;')
css = css.replace('height: fill-parent-flow(1);', 'flex: 1; min-height: 0; height: auto;')
css = css.replace('visibility: collapse;', 'display: none !important;')
css = css.replace('box-shadow: fill #000000b0 0px 14px 64px 0px;', 'box-shadow: 0 14px 64px #000000b0;')
css = re.sub(r'(horizontal|vertical)-align: [\w-]+;', '', css)
css = re.sub(r"@keyframes '([^']+)'", r'@keyframes \1', css)
body = element(ET.parse(LAYOUT).getroot().find('Panel'))
adapter = '''
* { box-sizing: border-box; flex-shrink: 0; }
body { margin: 0; overflow: hidden; font: 14px "Noto Sans CJK SC", "Microsoft YaHei", sans-serif; background: #151b20; color: #e4e8eb; }
button { position: relative; font: inherit; cursor: pointer; color: inherit; text-align: left; border: 0; background: transparent; }
button:focus-visible { outline: 1px solid #98c8bc; outline-offset: 2px; }
span { display: block; }
div { position: relative; }
button:disabled { cursor: default; }
#stage { width: 1920px; height: 1080px; position: absolute; transform-origin: top left; }
.game { position: absolute; inset: 0; overflow: hidden; background: radial-gradient(ellipse at 60% 35%, #39454c, #252f38 45%, #131c22 85%); }
.game:before { content: ''; position: absolute; inset: 0; background: repeating-linear-gradient(0deg, transparent, transparent 130px, #b8cdd706 131px), repeating-linear-gradient(90deg, transparent, transparent 160px, #b8cdd706 161px); transform: perspective(550px) rotateX(35deg) scale(2); }
.preview-label { position: absolute; top: 32px; left: 36px; font-size: 12px; letter-spacing: 2px; color: #71838f; }
.preview-note { position: absolute; bottom: 32px; width: 100%; text-align: center; color: #71838f; font-size: 12px; }
#sw_root { display: flex; align-items: center; justify-content: center; }
.root, #sw_root { pointer-events: none; }
.menu-box { pointer-events: auto; overflow: hidden; }
.window-header, .heading, .menu-footer, .item, .category { align-items: center; }
.map-thumbnail { position: relative; overflow: hidden; display: flex; align-items: center; }
.map-artwork { position: absolute; inset: 0; }
.category { display: flex; }
.nav-btn, .page-btn, .close-btn { display: flex; align-items: center; justify-content: center; }
.item-body { overflow: hidden; }
.menu-title, .item-text, .item-comment, #sw_operator, #sw_context { overflow: hidden; }
.footer-text { overflow: hidden; }
#toast { position: absolute; bottom: 112px; left: 50%; transform: translateX(-50%); padding: 12px 24px; border: 1px solid #98c8bc40; border-radius: 4px; background: #17211f; color: #b4cec5; display: none; }
#reopen { position: absolute; inset: 0; margin: auto; width: 200px; height: 48px; border: 1px solid #98c8bc40; border-radius: 4px; background: #17211f; text-align: center; display: none; }
'''
script = r'''
const $ = id => document.getElementById(id);
const text = (id, value) => { $(id).textContent = value; };
const maps = Object.entries({kz_angina:'3636272568',kz_angina_x:'3746453872',kz_antimony:'3609954118',kz_asteroid_beach:'3771237485',kz_avalon:'3583337319',kz_bayharborbhopper:'3593758954',kz_bhop_apricity:'3514400945',kz_bhop_majka:'3764093804',kz_bhop_slide:'3636997995'});
const people = ['XBDJ', 'Alice', '山海', 'KZ enjoyer', '夜航星', 'a_very_long_player_name_for_layout_check', '青空'];
const thumbnailClasses = {};
Promise.all(maps.map(async ([name,id])=>{ const hash=await crypto.subtle.digest('SHA-256',new TextEncoder().encode('workshop:'+id)); thumbnailClasses[id]='map-thumb-'+Array.from(new Uint8Array(hash)).map(x=>x.toString(16).padStart(2,'0')).join('').slice(0,16); })).then(()=>render());
const reservationMode = new URLSearchParams(location.search).get('menu')==='reservation';
const stack = [];
let activeCategory = 0;
const item = (name, comment, action, danger=false, disabled=false) => ({name, comment, action, danger, disabled});
function push(title, comment, items, layout='list') { stack.push({title,comment,items,layout,page:0}); render(); }
function toast(message) { text('toast', '预览：'+message); $('toast').style.display='block'; clearTimeout(window.toastTimer); window.toastTimer=setTimeout(()=>$('toast').style.display='none',3500); }
function selectCategory(index) { activeCategory=index; stack.length=0; if(index===0) players(); else mapList(); }
function home() { if(reservationMode) { stack.length=0; reservation(); } else selectCategory(0); }
function reservation() {
 const ckzNub=[2,3,null,5,1,6,4,7,8], ckzPro=[3,null,4,6,2,7,5,8,9], vnlNub=[4,null,2,6,3,7,5,8,9], vnlPro=[5,6,null,7,4,8,6,9,10]; // Illustrative preview data, not real map ratings.
 const items=maps.map(([name,id],i)=>({...item(name,'作者信息不可用 · '+name,()=>{
  toast('已预定 '+name+'，使用 !rtv 同意换图'); $('sw_root').classList.add('hidden'); $('reopen').style.display='block';
 }), map:true, ckzNub:ckzNub[i], ckzPro:ckzPro[i], vnlNub:vnlNub[i], vnlPro:vnlPro[i], workshopId:id}));
 push('预定下一张地图','点击地图即可预定。预定后，玩家使用 !rtv 同意换图。',items,'map-picker');
}
function players() { push('玩家操作','选择在线玩家，查看可用操作。受保护的玩家不可选择。',people.map((name,i)=>item(name+' ['+(i%2?'CT':'SPEC')+']',i===0?'目标受免疫等级保护。':'#'+i+'  ·  7656119800000000'+i,()=>actions(name),false,i===0))); }
function actions(name) { push('操作: '+name,'当前目标 · '+name,[
 item('踢出','断开连接，玩家仍可重新加入',()=>confirm('踢出 '+name+'?', '即将踢出 '+name+'。玩家将立即断开连接，之后仍可重新加入。','确认踢出',()=>{toast('已踢出 '+name);home();},true),true),
 item('封禁','选择时长，限制玩家加入服务器',()=>durations(name),true),
 ...['处决','拍击（10 伤害）','重生','冻结（5 秒）','禁麦（默认时长）','禁言（默认时长）','沉默（默认时长）'].map(action=>item(action,'',()=>toast('已执行'+action+' · '+name))) ]); }
function durations(name) { push('封禁: '+name,'选择封禁时长，下一步核对并确认。',['30 秒','60 秒','1 小时','1 天','3 天','7 天','30 天','永久'].map(duration=>item(duration,'',()=>confirm('封禁 '+name+'?','即将封禁 '+name+'，时长：'+duration+'。确认后将断开连接并限制加入服务器。','确认封禁',()=>{toast('已封禁 '+name+' · '+duration);home();},true)))); }
function mapList() {
 const items=maps.map(([name,id])=>item(name,'CKZ NUB ? / PRO ? | VNL NUB ? / PRO ? · Workshop ID: '+id,()=>confirm(
  '更换地图至 '+name+'?',
  '将切换至 '+name+'（'+id+'）。确认后开始 3 秒倒计时；未安装的工坊地图会先下载。',
  '确认更换地图',
  ()=>{toast('开始换图 · '+name);selectCategory(1);}
 )));
 push('地图管理','从配置的地图中选择。未安装的工坊地图会在确认后按需下载。',items);
}
function confirm(title, comment, label, action, danger=false) { push(title,comment,[item(label,'',action,danger),item('取消','返回上一步',()=>{stack.pop();render();})],'confirmation'); }
function render() {
 const frame=stack.at(-1), size=reservationMode?4:5, pages=Math.ceil(frame.items.length/size), rows=frame.items.slice(frame.page*size,(frame.page+1)*size);
 $('sw_root').className=(reservationMode?'':'sidebar ')+frame.layout;
 $('sw_sidebar').classList.toggle('hidden',reservationMode);
 text('sw_brand',reservationMode?'预定下一张地图':'管理员面板'); text('sw_operator','XBDJ'); text('sw_context', [reservationMode?'地图预定':'管理员面板',...stack.map(x=>x.title)].slice(-3).join('  /  '));
 text('sw_navigation','管理功能'); text('sw_sidebar_hint','选择功能，切换右侧操作内容。');
 for(let i=0;i<5;i++) {
  const el=$('sw_category'+i); el.className='category'+(i>1?' hidden':'')+(i===activeCategory?' selected':'');
  if(i<2) { text('sw_category'+i+'_text',i===0?'玩家操作':'地图管理'); el.onclick=()=>selectCategory(i); }
 }
 text('sw_title',frame.title); text('sw_comment_text',frame.comment); text('sw_count',frame.items.length+' 个选项'); text('sw_section',frame.layout==='confirmation'?'请核对操作信息':reservationMode?'可预定地图 · NUB 存点 / PRO 裸跳':'可用操作');
 text('sw_page',(frame.page+1)+' / '+pages); text('sw_footer','点击选项继续操作'); text('sw_home_text','首页'); text('sw_back_text','返回'); text('sw_close_text','关闭'); text('sw_prev_text','上一页'); text('sw_next_text','下一页');
 for(let i=0;i<5;i++) {
  const row=rows[i], el=$('sw_item'+i); el.className='item'+(!row?' hidden':'')+(row?.danger?' destructive':'')+(row?.disabled?' disabled':'')+(row?.map?' map-row':''); el.disabled=!row||!!row.disabled;
  $('sw_item'+i+'_thumbnail').classList.toggle('hidden',!row?.map); $('sw_item'+i+'_tiers').classList.toggle('hidden',!row?.map);
  $('sw_item'+i+'_artwork').className='map-artwork '+(thumbnailClasses[row?.workshopId]||'');
  for (const [suffix,key,label] of [['ckz_nub_tier','ckzNub','CKZ NUB'],['ckz_pro_tier','ckzPro','CKZ PRO'],['vnl_nub_tier','vnlNub','VNL NUB'],['vnl_pro_tier','vnlPro','VNL PRO']]) { text('sw_item'+i+'_'+suffix,row?.[key]?label+' 等级 '+row[key]:label+' 等级未知'); } text('sw_item'+i+'_no_image','暂无图片');
  if(!row) continue;
  text('sw_item'+i+'_text',row.name); text('sw_item'+i+'_comment',row.comment); text('sw_item'+i+'_action',row.disabled?'不可用':'选择');
  $('sw_item'+i+'_comment').classList.toggle('hidden',!row.comment); el.onclick=row.action;
 }
 $('sw_home').classList.add('hidden'); $('sw_back').classList.toggle('hidden',stack.length<2);
 $('sw_prev').disabled=frame.page===0; $('sw_next').disabled=frame.page===pages-1;
 $('sw_prev').classList.toggle('disabled',$('sw_prev').disabled); $('sw_next').classList.toggle('disabled',$('sw_next').disabled);
}
$('sw_prev').onclick=()=>{stack.at(-1).page--;render();}; $('sw_next').onclick=()=>{stack.at(-1).page++;render();};
$('sw_back').onclick=()=>{stack.pop();render();};
$('sw_close').onclick=()=>{$('sw_root').classList.add('hidden');$('reopen').style.display='block';};
$('reopen').onclick=()=>{$('reopen').style.display='none';home();};
function resize(){const scale=Math.min(innerWidth/1920,innerHeight/1080);$('stage').style.transform='scale('+scale+')';$('stage').style.left=(innerWidth-1920*scale)/2+'px';$('stage').style.top=(innerHeight-1080*scale)/2+'px';}
window.addEventListener('resize',resize);home();resize();
'''
script += "\nif(reservationMode) { document.querySelector('.preview-note').textContent='地图预定预览 · 等级为示例数据 · 游戏内使用 !yd 打开'; text('reopen','重新打开 !yd'); }\n"
output = '<!doctype html><html lang="zh-CN"><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SWS2 HUD · 交互预览</title><style>'+css+'\n'+adapter+'</style><div id="stage"><div class="game"></div><div class="preview-label">SWS2 / CUSTOM HUD</div>'+body+'<div class="preview-note">界面交互预览 · 示例数据 · 游戏内使用 !admin 打开</div><div id="toast" role="status"></div><button id="reopen">重新打开 !admin</button></div><script>'+script+'</script></html>'
(ROOT/'preview').mkdir(exist_ok=True)
(ROOT/'preview/index.html').write_text(output)
(ROOT/'preview/reservation.html').write_text(output.replace(
    "const reservationMode = new URLSearchParams(location.search).get('menu')==='reservation';",
    'const reservationMode = true;'))
print('Generated preview/index.html and preview/reservation.html from Panorama XML and CSS')
