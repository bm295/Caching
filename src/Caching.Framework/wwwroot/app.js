const elements = {
    heroNode: document.getElementById("heroNode"),
    heroStorage: document.getElementById("heroStorage"),
    heroReplication: document.getElementById("heroReplication"),
    nodeCount: document.getElementById("nodeCount"),
    requestTimeout: document.getElementById("requestTimeout"),
    storageLibrary: document.getElementById("storageLibrary"),
    featureList: document.getElementById("featureList"),
    placementList: document.getElementById("placementList"),
    replicaList: document.getElementById("replicaList"),
    readResult: document.getElementById("readResult"),
    nodeList: document.getElementById("nodeList"),
    activityLog: document.getElementById("activityLog"),
    statusMessage: document.getElementById("statusMessage"),
    keyInput: document.getElementById("keyInput"),
    valueInput: document.getElementById("valueInput"),
    ttlInput: document.getElementById("ttlInput"),
    setButton: document.getElementById("setButton"),
    getButton: document.getElementById("getButton"),
    deleteButton: document.getElementById("deleteButton"),
    inspectButton: document.getElementById("inspectButton"),
    sampleButton: document.getElementById("sampleButton"),
    clearLogButton: document.getElementById("clearLogButton")
};

const actionButtons = [
    elements.setButton,
    elements.getButton,
    elements.deleteButton,
    elements.inspectButton,
    elements.sampleButton
];

document.addEventListener("DOMContentLoaded", () => {
    wireEvents();
    loadSamplePayload();
    initialize();
});

async function initialize() {
    try {
        const overview = await fetchJson("/demo/api/overview");
        renderOverview(overview);
        setStatus("Dashboard ready. Inspecting the sample key.", "success");
        await inspectCurrentKey(false);
    } catch (error) {
        setStatus(error.message, "error");
        pushLog("Dashboard bootstrap failed.", error.message);
    }
}

function wireEvents() {
    elements.setButton.addEventListener("click", () => runAction(setValue));
    elements.getButton.addEventListener("click", () => runAction(getValue));
    elements.deleteButton.addEventListener("click", () => runAction(deleteValue));
    elements.inspectButton.addEventListener("click", () => runAction(() => inspectCurrentKey(true)));
    elements.sampleButton.addEventListener("click", () => {
        loadSamplePayload();
        setStatus("Loaded a sample key, payload, and TTL.", "success");
    });
    elements.clearLogButton.addEventListener("click", () => {
        elements.activityLog.innerHTML = "";
        pushLog("Cleared the activity log.");
    });
}

async function runAction(action) {
    setBusy(true);

    try {
        await action();
    } catch (error) {
        setStatus(error.message, "error");
        pushLog("Request failed.", error.message);
    } finally {
        setBusy(false);
    }
}

async function setValue() {
    const key = requireKey();
    const value = elements.valueInput.value.trim();
    const ttlSeconds = parseOptionalInteger(elements.ttlInput.value);

    if (!value) {
        throw new Error("Value is required for SET.");
    }

    const response = await fetch(`/cache/${encodeURIComponent(key)}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            value,
            ttlSeconds
        })
    });

    await ensureSuccess(response, "SET request failed.");
    setStatus(`SET stored ${key} and replicated it to the configured owners.`, "success");
    pushLog(`SET ${key}`, ttlSeconds ? `TTL ${ttlSeconds}s` : "No TTL");
    await inspectCurrentKey(false);
}

async function getValue() {
    const key = requireKey();
    const response = await fetch(`/cache/${encodeURIComponent(key)}`, {
        headers: {
            "Accept": "application/json"
        }
    });

    if (response.status === 404) {
        renderReadResult(null);
        setStatus(`GET missed for ${key}.`, "error");
        pushLog(`GET ${key}`, "Miss");
        await refreshClusterViews(key);
        return;
    }

    await ensureSuccess(response, "GET request failed.");
    const payload = await response.json();

    renderReadResult(payload);
    setStatus(`GET returned a value from ${payload.nodeId}.`, "success");
    pushLog(`GET ${key}`, `Served by ${payload.nodeId}`);
    await refreshClusterViews(key);
}

async function deleteValue() {
    const key = requireKey();
    const response = await fetch(`/cache/${encodeURIComponent(key)}`, {
        method: "DELETE"
    });

    await ensureSuccess(response, "DELETE request failed.");
    setStatus(`DELETE removed ${key} from the owner replicas.`, "success");
    pushLog(`DELETE ${key}`);
    await inspectCurrentKey(false);
}

async function inspectCurrentKey(announce) {
    const key = requireKey();
    await refreshClusterViews(key);

    if (announce) {
        setStatus(`Inspection refreshed for ${key}.`, "success");
        pushLog(`INSPECT ${key}`);
    }
}

async function refreshClusterViews(key) {
    const [placement, inspection] = await Promise.all([
        fetchJson(`/cluster/placement/${encodeURIComponent(key)}`),
        fetchJson(`/demo/api/inspect/${encodeURIComponent(key)}`)
    ]);

    renderPlacement(placement.owners ?? []);
    renderReplicas(inspection.replicas ?? []);
    renderReadResult(inspection.readResult ?? null);
}

function renderOverview(overview) {
    elements.heroNode.textContent = overview.nodeId;
    elements.heroStorage.textContent = humanizeStorageMode(overview.storageMode);
    elements.heroReplication.textContent = `${overview.replicationFactor} replicas`;
    elements.nodeCount.textContent = String(overview.nodes.length);
    elements.requestTimeout.textContent = `${overview.requestTimeoutMilliseconds} ms`;
    elements.storageLibrary.textContent = overview.storageLibrary;

    elements.featureList.innerHTML = overview.features
        .map(feature => `
            <article class="chip">
                <strong>${escapeHtml(feature.name)}</strong>
                <span>${escapeHtml(feature.description)}</span>
            </article>
        `)
        .join("");

    elements.nodeList.innerHTML = overview.nodes
        .map(node => `
            <article class="stack-item">
                <div class="stack-head">
                    <strong>${escapeHtml(node.nodeId)}</strong>
                    <div class="stack-meta">
                        ${node.isCurrent ? '<span class="badge current">Current node</span>' : ""}
                    </div>
                </div>
                <div class="mono">${escapeHtml(node.url)}</div>
            </article>
        `)
        .join("");
}

function renderPlacement(owners) {
    if (!owners.length) {
        elements.placementList.innerHTML = renderEmpty("No placement data is available for the current key.");
        return;
    }

    elements.placementList.innerHTML = owners
        .map((owner, index) => `
            <article class="stack-item">
                <div class="stack-head">
                    <strong>${escapeHtml(owner.nodeId)}</strong>
                    <div class="stack-meta">
                        <span class="badge">${index === 0 ? "Primary owner" : `Replica ${index + 1}`}</span>
                    </div>
                </div>
                <div class="mono">${escapeHtml(owner.url)}</div>
            </article>
        `)
        .join("");
}

function renderReplicas(replicas) {
    if (!replicas.length) {
        elements.replicaList.innerHTML = renderEmpty("No owner replicas were returned.");
        return;
    }

    elements.replicaList.innerHTML = replicas
        .map(replica => `
            <article class="stack-item">
                <div class="stack-head">
                    <strong>${escapeHtml(replica.nodeId)}</strong>
                    <div class="stack-meta">
                        <span class="badge ${replica.hasValue ? "success" : "warn"}">
                            ${replica.hasValue ? "Value present" : "No value"}
                        </span>
                        ${replica.isLocalNode ? '<span class="badge current">Local store</span>' : ""}
                    </div>
                </div>
                <div class="mono">${escapeHtml(replica.baseAddress)}</div>
                ${replica.hasValue
                    ? `<pre>${escapeHtml(replica.value ?? "")}</pre>`
                    : '<div class="empty-state">Replica currently has no value for this key.</div>'}
            </article>
        `)
        .join("");
}

function renderReadResult(readResult) {
    if (!readResult) {
        elements.readResult.classList.add("missing");
        elements.readResult.innerHTML = `
            <strong>Cache miss</strong>
            <div class="empty-state">The coordinator could not read a value from any owner replica.</div>
        `;
        return;
    }

    elements.readResult.classList.remove("missing");
    elements.readResult.innerHTML = `
        <strong>${escapeHtml(readResult.nodeId)} served the current value</strong>
        <div class="stack-meta">
            <span class="badge ${readResult.isLocal ? "current" : ""}">
                ${readResult.isLocal ? "Local read" : "Peer read"}
            </span>
            <span class="badge">Key ${escapeHtml(readResult.key)}</span>
        </div>
        <pre>${escapeHtml(readResult.value)}</pre>
    `;
}

function renderEmpty(message) {
    return `
        <article class="stack-item">
            <div class="empty-state">${escapeHtml(message)}</div>
        </article>
    `;
}

function loadSamplePayload() {
    elements.keyInput.value = "demo:cart:42";
    elements.valueInput.value = JSON.stringify({
        customerId: "customer-42",
        lineItems: [
            { sku: "redis-book", quantity: 1 },
            { sku: "cache-demo", quantity: 2 }
        ],
        updatedUtc: new Date().toISOString()
    }, null, 2);
    elements.ttlInput.value = "90";
}

function pushLog(message, detail = "") {
    const item = document.createElement("li");
    const timestamp = new Date().toLocaleTimeString();
    item.innerHTML = `
        <time>${escapeHtml(timestamp)}</time>
        <strong>${escapeHtml(message)}</strong>
        ${detail ? `<div>${escapeHtml(detail)}</div>` : ""}
    `;

    elements.activityLog.prepend(item);

    while (elements.activityLog.children.length > 12) {
        elements.activityLog.removeChild(elements.activityLog.lastElementChild);
    }
}

function setStatus(message, mode) {
    elements.statusMessage.textContent = message;
    elements.statusMessage.className = "status-message";

    if (mode) {
        elements.statusMessage.classList.add(mode);
    }
}

function setBusy(isBusy) {
    actionButtons.forEach(button => {
        button.disabled = isBusy;
    });
}

function requireKey() {
    const key = elements.keyInput.value.trim();

    if (!key) {
        throw new Error("Cache key is required.");
    }

    return key;
}

function parseOptionalInteger(value) {
    const trimmed = value.trim();
    if (!trimmed) {
        return null;
    }

    const parsed = Number.parseInt(trimmed, 10);
    if (!Number.isInteger(parsed) || parsed <= 0) {
        throw new Error("TTL must be a positive integer.");
    }

    return parsed;
}

async function fetchJson(url) {
    const response = await fetch(url, {
        headers: {
            "Accept": "application/json"
        }
    });

    await ensureSuccess(response, `Request failed for ${url}.`);
    return response.json();
}

async function ensureSuccess(response, fallbackMessage) {
    if (response.ok) {
        return;
    }

    let message = fallbackMessage;

    try {
        const payload = await response.json();
        if (payload?.message) {
            message = payload.message;
        }
    } catch {
        if (response.statusText) {
            message = `${fallbackMessage} ${response.statusText}`;
        }
    }

    throw new Error(message);
}

function humanizeStorageMode(storageMode) {
    if (storageMode === "redis") {
        return "Redis-backed";
    }

    return "Memory fallback";
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}
