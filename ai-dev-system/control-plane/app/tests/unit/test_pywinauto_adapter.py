from __future__ import annotations

from app.agent.state import SelectorSpec
from app.automation.pywinauto_adapter import PywinautoAdapter


class _FakeWrapper:
    def __init__(self, handle: int, *, visible: bool = True) -> None:
        self.handle = handle
        self._visible = visible

    def is_visible(self) -> bool:
        return self._visible


class _FakeWindowSpec:
    def __init__(self, wrapper: _FakeWrapper) -> None:
        self._wrapper = wrapper

    def wrapper_object(self) -> _FakeWrapper:
        return self._wrapper


class _FakeDesktop:
    def __init__(self, *, backend: str, wrapper: _FakeWrapper | None) -> None:
        self.backend = backend
        self._wrapper = wrapper
        self.windows_calls: list[dict] = []

    def window(self, **criteria):
        if self._wrapper is None or criteria.get("handle") != self._wrapper.handle:
            raise LookupError("missing handle")
        return _FakeWindowSpec(self._wrapper)

    def windows(self, **criteria):
        self.windows_calls.append(criteria)
        return []


def test_resolve_window_prefers_direct_handle_lookup(monkeypatch) -> None:
    wrapper = _FakeWrapper(handle=101)
    fake_desktop = _FakeDesktop(backend="uia", wrapper=wrapper)

    monkeypatch.setattr("app.automation.pywinauto_adapter.Desktop", lambda backend: fake_desktop)

    resolved = PywinautoAdapter().resolve_window(SelectorSpec(handle=101, backend="uia"), backend="uia")

    assert resolved is wrapper
    assert fake_desktop.windows_calls == []


def test_resolve_window_falls_back_when_direct_handle_lookup_fails(monkeypatch) -> None:
    visible = _FakeWrapper(handle=202)

    class _FallbackDesktop(_FakeDesktop):
        def windows(self, **criteria):
            self.windows_calls.append(criteria)
            return [visible]

    fake_desktop = _FallbackDesktop(backend="uia", wrapper=None)
    monkeypatch.setattr("app.automation.pywinauto_adapter.Desktop", lambda backend: fake_desktop)

    resolved = PywinautoAdapter().resolve_window(SelectorSpec(handle=202, backend="uia"), backend="uia")

    assert resolved is visible
    assert fake_desktop.windows_calls == [{"handle": 202}]
