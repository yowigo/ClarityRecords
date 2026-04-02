(function () {
    'use strict';

    // ── 主入口 ────────────────────────────────────────────────────────────────
    function initGraph() {
        const container = document.getElementById('graph-container');
        if (!container) return;

        console.log('[graph] initGraph: fetching /api/graph-data...');
        container.innerHTML = '<div class="graph-loading">Loading nodes...</div>';

        fetch('/api/graph-data')
            .then(r => r.json())
            .then(data => {
                console.log('[graph] data received, nodes:', data.nodes?.length, 'links:', data.links?.length);
                renderGraph(container, data);
            })
            .catch(err => {
                console.error('[graph] fetch failed:', err);
                container.innerHTML = '<div class="graph-loading">图谱加载失败，请刷新重试</div>';
            });
    }

    // ── 渲染图谱 ──────────────────────────────────────────────────────────────
    function renderGraph(container, data) {
        container.innerHTML = '';

        if (!data.nodes || data.nodes.length === 0) {
            container.innerHTML = `
                <div class="graph-empty">
                    <p class="graph-empty-title">暂无文章节点</p>
                    <p class="graph-empty-desc">发布第一篇文章，开始构建你的知识图谱</p>
                    <a href="/" class="btn-primary" style="display:inline-flex">← 返回首页</a>
                </div>`;
            return;
        }

        const width  = container.clientWidth  || 800;
        const height = container.clientHeight || 600;

        // 预计算每个节点的连接度
        const degreeMap = {};
        data.links.forEach(l => {
            degreeMap[l.source] = (degreeMap[l.source] || 0) + 1;
            degreeMap[l.target] = (degreeMap[l.target] || 0) + 1;
        });
        data.nodes.forEach(n => { n.degree = degreeMap[n.id] || 0; });

        // ── SVG ──────────────────────────────────────────────────────────────
        const svg = d3.select(container)
            .append('svg')
            .attr('width',  '100%')
            .attr('height', '100%')
            .style('cursor', 'grab');

        const g = svg.append('g');

        // ── 缩放平移 ─────────────────────────────────────────────────────────
        const zoom = d3.zoom()
            .scaleExtent([0.1, 6])
            .on('zoom', e => g.attr('transform', e.transform));
        svg.call(zoom);

        // ── 边 ───────────────────────────────────────────────────────────────
        const link = g.append('g').attr('class', 'graph-links')
            .selectAll('line')
            .data(data.links)
            .enter()
            .append('line')
            .attr('stroke', '#e5e7eb')
            .attr('stroke-width', 1.5)
            .attr('stroke-opacity', 0.6);

        // ── 节点 ─────────────────────────────────────────────────────────────
        const node = g.append('g').attr('class', 'graph-nodes')
            .selectAll('g')
            .data(data.nodes)
            .enter()
            .append('g')
            .style('cursor', 'pointer')
            .call(d3.drag()
                .on('start', (e, d) => {
                    if (!e.active) sim.alphaTarget(0.3).restart();
                    d.fx = d.x; d.fy = d.y;
                    svg.style('cursor', 'grabbing');
                })
                .on('drag', (e, d) => { d.fx = e.x; d.fy = e.y; })
                .on('end',  (e, d) => {
                    if (!e.active) sim.alphaTarget(0);
                    d.fx = null; d.fy = null;
                    svg.style('cursor', 'grab');
                }))
            .on('click', (e, d) => {
                if (!e.defaultPrevented) {
                    window.location.href = '/articles/' + d.slug;
                }
            });

        const nodeR = d => 7 + Math.min(d.degree * 3, 15);

        node.append('circle')
            .attr('r', nodeR)
            .attr('fill', '#2563eb')
            .attr('fill-opacity', d => d.degree > 0 ? 0.88 : 0.5)
            .attr('stroke', '#f8f9fa')
            .attr('stroke-width', 2);

        node.append('text')
            .attr('dy', '0.32em')
            .attr('x', d => nodeR(d) + 4)
            .style('font-size',   '11px')
            .style('font-family', 'var(--mono, monospace)')
            .style('fill',        '#111827')
            .style('pointer-events', 'none')
            .style('user-select',    'none')
            .text(d => d.title.length > 22 ? d.title.slice(0, 22) + '…' : d.title);

        // ── 悬停高亮 ─────────────────────────────────────────────────────────
        node
            .on('mouseenter', function (_, d) {
                d3.select(this).select('circle')
                    .attr('stroke', '#2563eb').attr('stroke-width', 3);
                link
                    .attr('stroke', l =>
                        l.source.id === d.id || l.target.id === d.id ? '#2563eb' : '#e5e7eb')
                    .attr('stroke-opacity', l =>
                        l.source.id === d.id || l.target.id === d.id ? 1 : 0.15)
                    .attr('stroke-width', l =>
                        l.source.id === d.id || l.target.id === d.id ? 2 : 1.5);
            })
            .on('mouseleave', function () {
                d3.select(this).select('circle')
                    .attr('stroke', '#f8f9fa').attr('stroke-width', 2);
                link
                    .attr('stroke', '#e5e7eb')
                    .attr('stroke-opacity', 0.6)
                    .attr('stroke-width', 1.5);
            });

        // ── 力仿真 ───────────────────────────────────────────────────────────
        const sim = d3.forceSimulation(data.nodes)
            .force('link',      d3.forceLink(data.links).id(d => d.id).distance(130))
            .force('charge',    d3.forceManyBody().strength(-320))
            .force('center',    d3.forceCenter(width / 2, height / 2))
            .force('collision', d3.forceCollide(d => nodeR(d) + 8))
            .on('tick', () => {
                link
                    .attr('x1', d => d.source.x).attr('y1', d => d.source.y)
                    .attr('x2', d => d.target.x).attr('y2', d => d.target.y);
                node.attr('transform', d => `translate(${d.x},${d.y})`);
            });

        // 仿真结束后自动缩放到合适视野
        sim.on('end', () => {
            const bb = g.node().getBBox();
            if (!bb || bb.width === 0) return;
            const padding = 60;
            const scale = Math.min(
                (width  - padding * 2) / bb.width,
                (height - padding * 2) / bb.height,
                1.4
            );
            const tx = (width  - scale * (bb.x * 2 + bb.width))  / 2;
            const ty = (height - scale * (bb.y * 2 + bb.height)) / 2;
            svg.transition().duration(700)
                .call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
        });
    }

    // ── 移动端抽屉 ────────────────────────────────────────────────────────────
    window.toggleTraceDrawer = function () {
        const drawer = document.getElementById('trace-drawer-mobile');
        if (drawer) drawer.classList.toggle('open');
    };

    // ── 初始化时机 ────────────────────────────────────────────────────────────
    function tryInit() {
        if (document.getElementById('graph-container')) initGraph();
    }

    // 首次加载（直接访问 /graph 或 Ctrl+R）
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', tryInit);
    } else {
        tryInit();
    }

    // Blazor 增强导航：只检测 #graph-container 被新加入 DOM
    // 不监听子树修改，避免 initGraph 自身的 DOM 操作触发循环
    const _observer = new MutationObserver((mutations) => {
        for (const m of mutations) {
            // Blazor morph 可能把已有元素的 id 改成 graph-container（属性变更）
            if (m.type === 'attributes' && m.target.id === 'graph-container') {
                initGraph();
                return;
            }
            // 也处理真正新增节点的情况
            for (const node of m.addedNodes) {
                if (node.nodeType !== 1) continue;
                if (node.id === 'graph-container' || node.querySelector?.('#graph-container')) {
                    initGraph();
                    return;
                }
            }
        }
    });
    _observer.observe(document.documentElement, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['id']
    });

    // 暴露全局
    window.initGraph = initGraph;
})();
