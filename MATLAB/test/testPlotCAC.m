function testPlotCAC(dataPareto)
% plotPareto 画Pareto图
% headline 大标题
% subTitle1 2 3 4 子标题
% xLabelName yLabelName zLabelName 坐标轴名
% size 数据总数
% data  数据
% dataPareto Pareto最优数据
sz = 12;
xData = dataPareto(:, 2);
yData = dataPareto(:, 1);
N = dataPareto(:, 3);

figure(8)

scatter(xData, yData, sz, getCmap(N, 0, 8), 'filled');
hold on

m = size(dataPareto, 1);
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 1) > dataPareto(j, 1)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 2);
pareto(1, 2) = dataPareto(1, 1);
for i = 2 : m
    while n > 0 && dataPareto(i, 2) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 2);
    pareto(n, 2) = dataPareto(i, 1);
end
scatter(pareto(1:n, 1), pareto(1:n, 2), sz, 'r');
hold on
plot(pareto(1:n, 1), pareto(1:n, 2), 'r');
grid on

end

function cmap = getCmap(c, min, max)
shift = 0.1;

n = length(c);
cmap = zeros(n, 3);
for i = 1 : n
    normal = (c(i)-min)/(max-min);
    delta = (1-shift)*normal;
    cmap(i, 1) = delta;
    cmap(i, 2) = shift+delta;
    cmap(i, 3) = shift+delta;
end
end