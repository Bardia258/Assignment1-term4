(() => {
    const municipality = document.getElementById('MunicipalityId');
    const position = document.getElementById('PositionId');
    const status = document.getElementById('position-status');
    if (!municipality || !position) return;
    let request;
    municipality.addEventListener('change', async () => {
        request?.abort();
        const current = new AbortController(); request = current;
        position.replaceChildren(new Option('Choose a position', ''));
        status.textContent = '';
        if (!municipality.value) { position.disabled = false; return; }
        position.disabled = true;
        try {
            const response = await fetch(`${municipality.dataset.positionsUrl}?municipalityId=${encodeURIComponent(municipality.value)}`, { signal: current.signal });
            if (!response.ok) throw new Error('Unable to load positions.');
            const items = await response.json();
            for (const item of items) position.add(new Option(item.name, item.id));
            if (!items.length) status.textContent = 'Create a position in this municipality first.';
        } catch (error) {
            if (error.name !== 'AbortError') status.textContent = 'Could not load positions. Reload the page and try again.';
        } finally { if (!current.signal.aborted) position.disabled = false; }
    });
})();
