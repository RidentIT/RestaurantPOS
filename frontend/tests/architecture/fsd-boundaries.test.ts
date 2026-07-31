import { describe, it, expect } from "vitest";

describe("FSD Architecture Boundaries", () => {
  it("enforces layer hierarchy specification", () => {
    const layers = ["shared", "entities", "features", "widgets", "app"];
    expect(layers.indexOf("shared")).toBeLessThan(layers.indexOf("entities"));
    expect(layers.indexOf("entities")).toBeLessThan(layers.indexOf("features"));
    expect(layers.indexOf("features")).toBeLessThan(layers.indexOf("widgets"));
    expect(layers.indexOf("widgets")).toBeLessThan(layers.indexOf("app"));
  });
});
